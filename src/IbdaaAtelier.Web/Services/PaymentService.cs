using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class PaymentService(AtelierDbContext db)
{
    private const decimal MaxAmount = 999999999999999.999m;
    private static readonly string[] Methods = ["Cash", "KNET", "Card", "BankTransfer", "Other"];

    public async Task<PaymentEntryPage> PrepareAsync(long orderId, int userId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order is null)
            throw new ArgumentException("Order not found.");

        var previousPayments = await db.OrderPayments.AsNoTracking()
            .Where(payment => payment.OrderId == orderId)
            .OrderBy(payment => payment.PaymentId).ToListAsync(ct);
        var refunded = await db.OrderRefunds.AsNoTracking()
            .Where(refund => refund.OrderId == orderId)
            .SumAsync(refund => (decimal?)refund.Amount, ct) ?? 0m;
        var paid = previousPayments.Sum(payment => payment.Amount) - refunded;
        var balance = Math.Max(0m, order.Total - paid);

        if (order.Status == "Cancelled" || balance <= 0)
            throw new InvalidOperationException("This order is cancelled or already paid in full.");

        var request = new PaymentRequest { OrderId = orderId, UserId = userId };
        db.PaymentRequests.Add(request);
        await db.SaveChangesAsync(ct);

        return new PaymentEntryPage
        {
            OrderId = orderId,
            OrderNumber = order.OrderNumber,
            Total = order.Total,
            Paid = paid,
            Balance = balance,
            Refunded = refunded,
            PreviousPayments = previousPayments,
            Form = new PaymentForm
            {
                RequestId = request.RequestId,
                OrderId = orderId,
                Lines = Methods.Select(method => new PaymentLine { Method = method }).ToList()
            }
        };
    }

    public async Task<PaymentResult> SaveAsync(PaymentForm form, int userId, CancellationToken ct)
    {
        if (form.OrderId <= 0 || form.RequestId <= 0 ||
            form.Lines is null || form.Lines.Count is < 1 or > 5)
            throw new ArgumentException("Invalid payment request.");

        if (form.Lines.Any(line => line is null ||
            !Methods.Contains(line.Method, StringComparer.Ordinal) ||
            line.Amount < 0 || line.Amount > MaxAmount ||
            line.Amount != decimal.Round(line.Amount, 3) ||
            (line.Tendered.HasValue && (line.Tendered < 0 ||
                line.Tendered > MaxAmount ||
                line.Tendered != decimal.Round(line.Tendered.Value, 3)))))
            throw new ArgumentException("Use valid payment methods and KWD values with three decimals.");

        if (form.Lines.Select(line => line.Method).Distinct().Count() != form.Lines.Count)
            throw new ArgumentException("Payment method must appear only once per split payment.");

        var used = form.Lines.Where(line => line.Amount > 0).ToList();
        if (used.Count == 0 || form.Lines.Any(line =>
                line.Amount == 0 && line.Tendered is > 0))
            throw new ArgumentException("Enter at least one positive payment amount.");

        var prepared = new List<OrderPayment>(used.Count);
        foreach (var line in used)
        {
            var tendered = line.Tendered ?? line.Amount;
            if (tendered < line.Amount || (line.Method != "Cash" && tendered != line.Amount))
                throw new ArgumentException("Cash received must cover the payment amount. Other methods cannot have change.");

            prepared.Add(new OrderPayment
            {
                Method = line.Method,
                Amount = line.Amount,
                Tendered = tendered,
                Change = tendered - line.Amount,
                ReceivedByUserId = userId,
                OrderId = form.OrderId
            });
        }

        var amount = prepared.Aggregate(0m, (sum, line) => checked(sum + line.Amount));
        if (amount > MaxAmount)
            throw new ArgumentException("Payment amount exceeds the permitted limit.");

        var payload = JsonSerializer.Serialize(form);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);

        var request = await db.PaymentRequests.FromSqlInterpolated(
            $"SELECT * FROM dbo.PaymentRequests WITH (UPDLOCK, HOLDLOCK) WHERE RequestId = {form.RequestId}")
            .SingleOrDefaultAsync(ct);
        if (request is null || request.OrderId != form.OrderId || request.UserId != userId)
            throw new ArgumentException("Payment request does not belong to this order and account.");

        var existing = await db.PaymentBatches.AsNoTracking()
            .SingleOrDefaultAsync(batch => batch.RequestId == form.RequestId, ct);
        if (existing is not null)
        {
            if (request.Fingerprint != fingerprint)
                throw new InvalidOperationException("Payment request was already used with different amounts.");

            var currentOrder = await db.Orders.AsNoTracking()
                .SingleAsync(o => o.OrderId == form.OrderId, ct);
            var currentPaid = await GetPaidAsync(form.OrderId, ct);
            await transaction.CommitAsync(ct);
            return new PaymentResult(existing.PaymentBatchId, existing.Total,
                Math.Max(0m, currentOrder.Total - currentPaid), true);
        }
        if (request.Fingerprint is not null)
            throw new InvalidOperationException("Incomplete payment request requires review.");

        // A register close holds the same row lock, preventing unassigned late postings.
        var register = await db.CashRegisterSessions.FromSqlRaw(
            "SELECT * FROM dbo.CashRegisterSessions WITH (UPDLOCK, HOLDLOCK) WHERE ClosedAtUtc IS NULL")
            .SingleOrDefaultAsync(ct);
        var openingRequired = await db.ShopSettings.AsNoTracking()
            .Select(s => (bool?)s.RequireRegisterOpen).SingleOrDefaultAsync(ct) ?? false;
        if (openingRequired && register is null)
            throw new InvalidOperationException("Open the cash register before receiving payments.");
        foreach (var payment in prepared)
            payment.RegisterSessionId = register?.SessionId;

        // Serialize all payments on one order, even if they use different request IDs.
        var order = await db.Orders.FromSqlInterpolated(
            $"SELECT * FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE OrderId = {form.OrderId}")
            .SingleOrDefaultAsync(ct);
        if (order is null || order.Status == "Cancelled")
            throw new InvalidOperationException("Cannot accept payment for a cancelled or missing order.");

        var paid = await GetPaidAsync(form.OrderId, ct);
        var balance = order.Total - paid;
        if (balance <= 0 || amount > balance)
            throw new InvalidOperationException("Payment exceeds the outstanding order balance.");

        var batch = new PaymentBatch
        {
            RequestId = request.RequestId,
            OrderId = order.OrderId,
            Total = amount
        };
        request.Fingerprint = fingerprint;
        db.PaymentBatches.Add(batch);
        await db.SaveChangesAsync(ct);

        foreach (var payment in prepared)
        {
            payment.PaymentBatchId = batch.PaymentBatchId;
            db.OrderPayments.Add(payment);
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Payment.Receive",
            EntityName = "PaymentBatch",
            EntityId = batch.PaymentBatchId
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new PaymentResult(batch.PaymentBatchId, amount, balance - amount, false);
    }

    public async Task<decimal> GetPaidAsync(long orderId, CancellationToken ct)
    {
        var received = await db.OrderPayments.AsNoTracking()
            .Where(payment => payment.OrderId == orderId)
            .SumAsync(payment => (decimal?)payment.Amount, ct) ?? 0m;
        var refunded = await db.OrderRefunds.AsNoTracking()
            .Where(refund => refund.OrderId == orderId)
            .SumAsync(refund => (decimal?)refund.Amount, ct) ?? 0m;
        return received - refunded;
    }
}

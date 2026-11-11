using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class RefundService(AtelierDbContext db)
{
    private static readonly string[] Methods = ["Cash", "KNET", "Card", "BankTransfer", "Other"];

    public async Task<RefundEntryPage> PrepareAsync(long orderId, int userId, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(o => o.OrderId == orderId, ct);
        if (order is null)
            throw new ArgumentException("Order not found.");

        var received = await db.OrderPayments.AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .GroupBy(p => p.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);
        var refunds = await db.OrderRefunds.AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .GroupBy(r => r.Method)
            .Select(g => new { Method = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var refundedByMethod = refunds.ToDictionary(r => r.Method, r => r.Amount);
        var available = received.ToDictionary(r => r.Method,
            r => Math.Max(0m, r.Amount - refundedByMethod.GetValueOrDefault(r.Method)));

        if (!available.Any(item => item.Value > 0))
            throw new InvalidOperationException("This order has no refundable payment balance.");

        var request = new RefundRequest { OrderId = orderId, UserId = userId };
        db.RefundRequests.Add(request);
        await db.SaveChangesAsync(ct);

        return new RefundEntryPage
        {
            OrderId = orderId,
            OrderNumber = order.OrderNumber,
            TotalPaid = received.Sum(r => r.Amount),
            TotalRefunded = refunds.Sum(r => r.Amount),
            AvailableByMethod = available,
            Form = new RefundForm { OrderId = orderId, RequestId = request.RequestId,
                Method = available.First(item => item.Value > 0).Key }
        };
    }

    public async Task<RefundResult> SaveAsync(RefundForm form, int userId, CancellationToken ct)
    {
        form.Reason = form.Reason?.Trim() ?? "";
        if (form.RequestId <= 0 || form.OrderId <= 0 ||
            !Methods.Contains(form.Method, StringComparer.Ordinal) ||
            form.Reason.Length is < 10 or > 500 ||
            form.Amount <= 0 || form.Amount > 999999999999999.999m ||
            form.Amount != decimal.Round(form.Amount, 3))
            throw new ArgumentException("Provide an approved payment method, positive KWD amount and reason of at least 10 characters.");

        var payload = JsonSerializer.Serialize(form);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var request = await db.RefundRequests.FromSqlInterpolated(
            $"SELECT * FROM dbo.RefundRequests WITH (UPDLOCK, HOLDLOCK) WHERE RequestId = {form.RequestId}")
            .SingleOrDefaultAsync(ct);

        if (request is null || request.UserId != userId || request.OrderId != form.OrderId)
            throw new ArgumentException("Refund request does not belong to this account and order.");

        var previous = await db.OrderRefunds.AsNoTracking()
            .SingleOrDefaultAsync(r => r.RequestId == form.RequestId, ct);
        if (previous is not null)
        {
            if (request.Fingerprint != fingerprint)
                throw new InvalidOperationException("Refund request was already used for a different amount.");

            await transaction.CommitAsync(ct);
            return new RefundResult(previous.RefundId, previous.Amount, true);
        }
        if (request.Fingerprint is not null)
            throw new InvalidOperationException("Incomplete refund request requires review.");

        var register = await db.CashRegisterSessions.FromSqlRaw(
            "SELECT * FROM dbo.CashRegisterSessions WITH (UPDLOCK, HOLDLOCK) WHERE ClosedAtUtc IS NULL")
            .SingleOrDefaultAsync(ct);
        var openingRequired = await db.ShopSettings.AsNoTracking()
            .Select(s => (bool?)s.RequireRegisterOpen).SingleOrDefaultAsync(ct) ?? false;
        if (openingRequired && register is null)
            throw new InvalidOperationException("Open the cash register before issuing refunds.");

        // This lock is shared with PaymentService, preventing concurrent over-refunds.
        var order = await db.Orders.FromSqlInterpolated(
            $"SELECT * FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE OrderId = {form.OrderId}")
            .SingleOrDefaultAsync(ct);
        if (order is null)
            throw new ArgumentException("Order not found.");

        var received = await db.OrderPayments
            .Where(p => p.OrderId == form.OrderId && p.Method == form.Method)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var refunded = await db.OrderRefunds
            .Where(r => r.OrderId == form.OrderId && r.Method == form.Method)
            .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        if (received <= 0 || form.Amount > received - refunded)
            throw new InvalidOperationException("Refund exceeds the remaining payment for the selected method.");

        var refund = new OrderRefund
        {
            RequestId = request.RequestId,
            OrderId = order.OrderId,
            Method = form.Method,
            Amount = form.Amount,
            Reason = form.Reason,
            ApprovedByUserId = userId,
            RegisterSessionId = register?.SessionId
        };
        request.Fingerprint = fingerprint;
        db.OrderRefunds.Add(refund);
        await db.SaveChangesAsync(ct);

        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Payment.Refund",
            EntityName = "OrderRefund",
            EntityId = refund.RefundId
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new RefundResult(refund.RefundId, refund.Amount, false);
    }
}

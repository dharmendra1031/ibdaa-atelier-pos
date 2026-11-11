using System.Data;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class OrderStatusService(AtelierDbContext db)
{
    public static string[] Next(string status) => status switch
    {
        "New" => ["Confirmed", "Cancelled"],
        "Confirmed" => ["In Processing", "Cancelled"],
        "In Processing" => ["Ready for Delivery", "Cancelled"],
        "Ready for Delivery" => ["Delivered", "Cancelled"],
        _ => []
    };

    public async Task ChangeAsync(OrderStatusForm form, int userId, CancellationToken ct)
    {
        if (form.OrderId <= 0)
            throw new ArgumentException("Choose an existing order.");

        form.Status = form.Status?.Trim() ?? "";
        form.Remarks = form.Remarks?.Trim();
        if (form.Remarks?.Length > 500 ||
            (form.Status == "Cancelled" && (form.Remarks?.Length ?? 0) < 10))
            throw new ArgumentException("Cancellation requires a reason of at least 10 characters.");

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var order = await db.Orders.FromSqlInterpolated(
            $"SELECT * FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE OrderId = {form.OrderId}")
            .SingleOrDefaultAsync(ct);
        if (order is null)
            throw new ArgumentException("Order not found.");

        if (!Next(order.Status).Contains(form.Status, StringComparer.Ordinal))
            throw new InvalidOperationException("Status transition is not permitted.");

        if (form.Status == "Cancelled")
        {
            var received = await db.OrderPayments
                .Where(p => p.OrderId == form.OrderId)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
            var refunded = await db.OrderRefunds
                .Where(r => r.OrderId == form.OrderId)
                .SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;
            if (received != refunded)
                throw new InvalidOperationException(
                    "Refund all received payments before cancelling this order.");
        }

        var previous = order.Status;
        order.Status = form.Status;
        db.OrderStatusHistory.Add(new OrderStatusChange
        {
            OrderId = order.OrderId,
            UserId = userId,
            OldStatus = previous,
            NewStatus = form.Status,
            Remarks = form.Remarks
        });
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = form.Status == "Cancelled" ? "Order.Cancel" : "Order.StatusChange",
            EntityName = "Order",
            EntityId = order.OrderId
        });

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}

using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Models.Security;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Services;

public sealed class PosOrderService(AtelierDbContext db, OrderLineBuilder lineBuilder)
{
    public async Task<long> ReserveAsync(int userId, CancellationToken ct)
    {
        var request = new OrderRequest { UserId = userId };
        db.OrderRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request.RequestId;
    }

    public async Task<SavedOrder> SaveAsync(OrderSaveForm form, int userId, CancellationToken ct)
    {
        if (form.RequestId <= 0 || form.CustomerId <= 0 ||
            form.Items is null || form.Items.Count is < 1 or > 30)
            throw new ArgumentException("Select a customer and at least one order item.");

        var todayInKuwait = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        if (form.DeliveryDate < todayInKuwait)
            throw new ArgumentException("Delivery date cannot be in the past.");

        var payload = JsonSerializer.Serialize(form);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var request = await db.OrderRequests.FromSqlInterpolated(
                $"SELECT * FROM dbo.OrderRequests WITH (UPDLOCK, HOLDLOCK) WHERE RequestId = {form.RequestId}")
            .SingleOrDefaultAsync(ct);
        if (request is null || request.UserId != userId)
            throw new ArgumentException("The order request does not belong to this account.");

        var existing = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(o => o.RequestId == request.RequestId, ct);
        if (existing is not null)
        {
            if (request.Fingerprint != fingerprint)
                throw new InvalidOperationException("The request was already used for a different order.");

            await transaction.CommitAsync(ct);
            return new SavedOrder(existing.OrderId, existing.OrderNumber, existing.Total, true);
        }

        if (request.Fingerprint is not null)
            throw new InvalidOperationException("An incomplete prior request needs administrator review.");

        var customer = await db.Customers.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerId == form.CustomerId, ct);
        if (customer is null)
            throw new ArgumentException("Select an existing customer.");

        var lines = await lineBuilder.BuildAsync(form.Items, ct);
        const decimal maxAmount = 999999999999999.999m;
        if (lines.Any(x => x.Item.LineTotal > maxAmount))
            throw new ArgumentException("Order line exceeds the allowed amount.");

        var total = lines.Aggregate(0m, (sum, x) => checked(sum + x.Item.LineTotal));
        if (total > maxAmount)
            throw new ArgumentException("Order total exceeds the allowed amount.");

        var order = new Order
        {
            RequestId = request.RequestId,
            CustomerId = customer.CustomerId,
            CreatedByUserId = userId,
            CustomerName = customer.Name,
            CustomerNameAr = customer.NameAr,
            CustomerMobile = customer.Mobile,
            DeliveryDate = form.DeliveryDate,
            Total = total
        };

        request.Fingerprint = fingerprint;
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        var shop = await db.ShopSettings.AsNoTracking().SingleOrDefaultAsync(ct);
        db.OrderInvoiceSnapshots.Add(new OrderInvoiceSnapshot
        {
            OrderId = order.OrderId,
            BusinessName = shop?.BusinessName ?? "IBDAA Atelier",
            BusinessNameAr = shop?.BusinessNameAr ?? "",
            Address = shop?.Address,
            AddressAr = shop?.AddressAr,
            Mobile = shop?.Mobile,
            Email = shop?.Email,
            LogoPath = shop?.LogoPath,
            ReceiptFooter = shop?.ReceiptFooter
        });

        foreach (var line in lines)
        {
            line.Item.OrderId = order.OrderId;
            db.OrderItems.Add(line.Item);
        }
        await db.SaveChangesAsync(ct);

        foreach (var line in lines)
        {
            foreach (var measurement in line.Measurements)
            {
                measurement.OrderItemId = line.Item.OrderItemId;
                db.OrderItemMeasurements.Add(measurement);
            }
        }

        db.OrderStatusHistory.Add(new OrderStatusChange
        {
            OrderId = order.OrderId,
            UserId = userId,
            NewStatus = "New"
        });
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = "Order.Create",
            EntityName = "Order",
            EntityId = order.OrderId
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new SavedOrder(order.OrderId, order.OrderNumber, order.Total, false);
    }
}

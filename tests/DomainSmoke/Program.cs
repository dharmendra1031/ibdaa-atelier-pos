using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var connection = Environment.GetEnvironmentVariable("IBDAA_CI_CONNECTION");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("IBDAA_CI_CONNECTION is required for isolated integration tests.");

var settings = new SqlConnectionStringBuilder(connection);
if (!string.Equals(settings.InitialCatalog, "IBDAA_CI", StringComparison.Ordinal) ||
    !string.Equals(settings.DataSource, "127.0.0.1,14333", StringComparison.Ordinal))
    throw new InvalidOperationException("Integration tests are restricted to the local disposable IBDAA_CI database.");

var options = new DbContextOptionsBuilder<AtelierDbContext>()
    .UseSqlServer(connection)
    .Options;

await using var db = new AtelierDbContext(options);
Check(await db.Database.CanConnectAsync(), "SQL Server test database is unavailable.");
Check(!await db.Orders.AnyAsync(), "Expected a fresh isolated test database.");

var user = new ShopUser
{
    UserName = "ci_order_admin",
    PasswordHash = "Test fixture only - not a usable login",
    Role = "Admin"
};
var customer = new Customer
{
    Name = "Integration Customer",
    NameAr = "عميل اختبار التكامل",
    Mobile = "28881234"
};
var category = new ProductCategory { Name = "Integration Clothing", NameAr = "ملابس اختبار" };
var serviceCategory = new ServiceCategory { Name = "Integration Alterations", NameAr = "تعديلات اختبار" };

db.Users.Add(user);
db.Customers.Add(customer);
db.ProductCategories.Add(category);
db.ServiceCategories.Add(serviceCategory);
await db.SaveChangesAsync();

var product = new Product
{
    CategoryId = category.CategoryId,
    Name = "Integration Garment",
    NameAr = "ثوب اختبار",
    RegularPrice = 25.000m
};
var service = new AlterationService
{
    ServiceCategoryId = serviceCategory.ServiceCategoryId,
    Name = "Integration Hem",
    NameAr = "تقصير اختبار",
    Price = 2.500m
};
db.Products.Add(product);
db.AlterationServices.Add(service);
await db.SaveChangesAsync();

var size = new ProductSize { ProductId = product.ProductId, Name = "M" };
var chest = new MeasurementDefinition
{
    ProductId = product.ProductId,
    Name = "Chest",
    NameAr = "الصدر",
    Unit = "cm",
    IsRequired = true
};
db.ProductSizes.Add(size);
db.MeasurementDefinitions.Add(chest);
var inheritedChest = new MeasurementDefinition
{
    CategoryId = category.CategoryId,
    Name = "Chest",
    NameAr = "الصدر العام",
    Unit = "in",
    IsRequired = true
};
db.MeasurementDefinitions.Add(inheritedChest);
await db.SaveChangesAsync();

var visibleFields = MeasurementFields.Resolve(
    new[] { inheritedChest, chest }, product.ProductId, category.CategoryId);
Check(visibleFields.Count == 1 && visibleFields[0].MeasurementId == chest.MeasurementId,
    "Product-specific measurement must override an identically named category field.");

var orders = new PosOrderService(db, new OrderLineBuilder(db));
var payments = new PaymentService(db);
var refunds = new RefundService(db);
var statuses = new OrderStatusService(db);
var orderRequestId = await orders.ReserveAsync(user.UserId, CancellationToken.None);
Check(orderRequestId > 0, "Order request must have a numeric SQL identity.");

var orderForm = new OrderSaveForm
{
    RequestId = orderRequestId,
    CustomerId = customer.CustomerId,
    DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3).AddDays(1)),
    Items =
    [
        new OrderLineInput
        {
            ItemType = "Product",
            ItemId = product.ProductId,
            SizeId = size.SizeId,
            Quantity = 1,
            Measurements = [new OrderMeasurementInput { MeasurementId = chest.MeasurementId, Value = 102m }]
        },
        new OrderLineInput { ItemType = "Service", ItemId = service.ServiceId, Quantity = 1 }
    ]
};
var saved = await orders.SaveAsync(orderForm, user.UserId, CancellationToken.None);
Check(saved.OrderId > 0 && saved.Total == 27.500m && !saved.Replayed, "Order total or save is incorrect.");
Check(saved.OrderNumber == $"IA-{saved.OrderId}", "Order number does not match numeric ID.");

var duplicateOrder = await orders.SaveAsync(orderForm, user.UserId, CancellationToken.None);
Check(duplicateOrder.Replayed && duplicateOrder.OrderId == saved.OrderId, "Retry created a second order.");
Check(await db.Orders.CountAsync() == 1 && await db.OrderItems.CountAsync() == 2,
    "Order retry changed persisted item count.");
Check(await db.OrderItemMeasurements.CountAsync() == 1,
    "Item-specific measurement snapshot is missing.");
Check(await db.OrderInvoiceSnapshots.AnyAsync(s => s.OrderId == saved.OrderId),
    "Invoice branding snapshot is missing.");

await ExpectFailureAsync<InvalidOperationException>(async () =>
{
    var changed = new OrderSaveForm
    {
        RequestId = orderForm.RequestId,
        CustomerId = orderForm.CustomerId,
        DeliveryDate = orderForm.DeliveryDate,
        Items = [new OrderLineInput { ItemType = "Service", ItemId = service.ServiceId, Quantity = 1 }]
    };
    await orders.SaveAsync(changed, user.UserId, CancellationToken.None);
}, "A reused order request accepted a changed payload.");

var firstPayment = await payments.PrepareAsync(saved.OrderId, user.UserId, CancellationToken.None);
var splitForm = new PaymentForm
{
    RequestId = firstPayment.Form.RequestId,
    OrderId = saved.OrderId,
    Lines =
    [
        new PaymentLine { Method = "Cash", Amount = 10.000m, Tendered = 12.000m },
        new PaymentLine { Method = "KNET", Amount = 10.000m, Tendered = 10.000m }
    ]
};
var split = await payments.SaveAsync(splitForm, user.UserId, CancellationToken.None);
Check(split.Received == 20.000m && split.Balance == 7.500m && !split.Replayed,
    "Split payment, cash change or advance balance is wrong.");

var splitRetry = await payments.SaveAsync(splitForm, user.UserId, CancellationToken.None);
Check(splitRetry.Replayed && splitRetry.BatchId == split.BatchId, "Split payment retry was not idempotent.");
Check(await db.OrderPayments.CountAsync() == 2 && await db.PaymentBatches.CountAsync() == 1,
    "Split payment retry generated duplicate ledger records.");
Check(await db.OrderPayments.Where(p => p.Method == "Cash")
    .Select(p => p.Change).SingleAsync() == 2.000m, "Cash change must not be counted as revenue.");

await ExpectFailureAsync<InvalidOperationException>(() =>
    payments.SaveAsync(new PaymentForm
    {
        RequestId = splitForm.RequestId,
        OrderId = splitForm.OrderId,
        Lines = [new PaymentLine { Method = "Cash", Amount = 11.000m, Tendered = 11.000m }]
    }, user.UserId, CancellationToken.None), "Payment request accepted a different retry amount.");

var secondPayment = await payments.PrepareAsync(saved.OrderId, user.UserId, CancellationToken.None);
await ExpectFailureAsync<InvalidOperationException>(() =>
    payments.SaveAsync(new PaymentForm
    {
        RequestId = secondPayment.Form.RequestId,
        OrderId = saved.OrderId,
        Lines = [new PaymentLine { Method = "Cash", Amount = 7.501m }]
    }, user.UserId, CancellationToken.None), "Payment above outstanding balance was accepted.");

var finalPayment = await payments.SaveAsync(new PaymentForm
{
    RequestId = secondPayment.Form.RequestId,
    OrderId = saved.OrderId,
    Lines = [new PaymentLine { Method = "Cash", Amount = 7.500m }]
}, user.UserId, CancellationToken.None);
Check(finalPayment.Balance == 0m && await payments.GetPaidAsync(saved.OrderId, CancellationToken.None) == 27.500m,
    "Full payment did not close the outstanding balance.");

var refundRequest = await refunds.PrepareAsync(saved.OrderId, user.UserId, CancellationToken.None);
var refundForm = new RefundForm
{
    RequestId = refundRequest.Form.RequestId,
    OrderId = saved.OrderId,
    Method = "Cash",
    Amount = 4.000m,
    Reason = "Customer-approved refund for test item"
};
var refund = await refunds.SaveAsync(refundForm, user.UserId, CancellationToken.None);
Check(refund.Refunded == 4.000m && !refund.Replayed, "Refund was not saved.");
var refundRetry = await refunds.SaveAsync(refundForm, user.UserId, CancellationToken.None);
Check(refundRetry.Replayed && refundRetry.RefundId == refund.RefundId,
    "Refund retry created a second refund.");
Check(await db.OrderRefunds.CountAsync() == 1 &&
      await payments.GetPaidAsync(saved.OrderId, CancellationToken.None) == 23.500m,
    "Net paid amount after refund is incorrect.");

await ExpectFailureAsync<InvalidOperationException>(() =>
    statuses.ChangeAsync(new OrderStatusForm
    {
        OrderId = saved.OrderId,
        Status = "Cancelled",
        Remarks = "Attempt to cancel a partially paid order"
    }, user.UserId, CancellationToken.None), "Paid order was cancelled without full refund.");

await statuses.ChangeAsync(new OrderStatusForm
{
    OrderId = saved.OrderId, Status = "Confirmed"
}, user.UserId, CancellationToken.None);
Check(await db.OrderStatusHistory.CountAsync(x => x.OrderId == saved.OrderId) == 2,
    "Order status transition was not audited.");

Console.WriteLine("Domain integration passed: numeric order, snapshots, duplicate retries, split/partial/full payments, cash change, refunds and status safeguards.");

static void Check(bool condition, string message)
{
    if (!condition)
        throw new Exception(message);
}

static async Task ExpectFailureAsync<T>(Func<Task> action, string message) where T : Exception
{
    try
    {
        await action();
    }
    catch (T)
    {
        return;
    }

    throw new Exception(message);
}

using IbdaaAtelier.Web.Data;
using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Models.Registers;
using IbdaaAtelier.Web.Models.Printing;
using System.Security.Cryptography;
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
db.ShopSettings.Add(new ShopSettings
{
    BusinessName = "Integration Test Atelier",
    BusinessNameAr = "مشغل اختبار",
    ReceiptFooter = "Thank you from TEST shop",
    RequireRegisterOpen = true
});
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

await using (var productCheck = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
{
    var duplicate = await db.Products.FromSqlInterpolated(
        $"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE CategoryId = {category.CategoryId} AND Name = {product.Name} AND ProductType = {product.ProductType}")
        .AsNoTracking().AnyAsync();
    Check(duplicate, "Product duplicate guard did not detect an existing product.");

    var distinct = await db.Products.FromSqlInterpolated(
        $"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE CategoryId = {category.CategoryId} AND Name = {"Another Garment"} AND ProductType = {product.ProductType}")
        .AsNoTracking().AnyAsync();
    Check(!distinct, "Product duplicate guard rejected a different product.");
    await productCheck.CommitAsync();
}

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
Check(saved.OrderId > 0, "Invoice number must be a positive numeric ID.");

var printToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var anotherToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
var terminal = new PrintTerminal
{
    Name = "Cashier PC",
    TokenHash = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(printToken)))
};
var anotherTerminal = new PrintTerminal
{
    Name = "Second PC",
    TokenHash = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(anotherToken)))
};
db.PrintTerminals.AddRange(terminal, anotherTerminal);
await db.SaveChangesAsync();
Check(terminal.TerminalId > 0 && anotherTerminal.TerminalId > 0 &&
    terminal.TerminalId != anotherTerminal.TerminalId, "Printer terminal IDs must be numeric and unique.");

var queue = new PrintQueueService(db);
Check(await queue.AuthenticateAsync(terminal.TerminalId, printToken, CancellationToken.None),
    "Correct terminal token was rejected.");
Check(!await queue.AuthenticateAsync(terminal.TerminalId, anotherToken, CancellationToken.None),
    "A foreign terminal token was accepted.");
var printJob = await queue.QueueAsync(saved.OrderId, terminal.TerminalId, CancellationToken.None);
Check(printJob.PrintJobId > 0 && printJob.Status == "Pending", "Print job was not queued.");
Check((await queue.QueueAsync(saved.OrderId, terminal.TerminalId, CancellationToken.None)).PrintJobId ==
    printJob.PrintJobId, "Repeated save queued a duplicate print.");
Check((await queue.ClaimAsync(terminal.TerminalId, CancellationToken.None))?.PrintJobId ==
    printJob.PrintJobId, "Agent could not claim its job.");
Check(await queue.ClaimAsync(terminal.TerminalId, CancellationToken.None) is null,
    "A claimed print job was delivered twice.");
Check(await queue.GetClaimedAsync(anotherTerminal.TerminalId, printJob.PrintJobId, CancellationToken.None)
    is null, "Another terminal accessed a print job.");
Check(!await queue.CompleteAsync(anotherTerminal.TerminalId, printJob.PrintJobId, true, null,
    CancellationToken.None), "Another terminal completed a print job.");
Check(await queue.CompleteAsync(terminal.TerminalId, printJob.PrintJobId, true, null,
    CancellationToken.None), "Agent result was not recorded.");
Check((await queue.QueueAsync(saved.OrderId, terminal.TerminalId, CancellationToken.None)).Status ==
    "Completed", "Completed print job was automatically queued again.");

var failedJob = await queue.QueueAsync(saved.OrderId, anotherTerminal.TerminalId, CancellationToken.None);
await queue.ClaimAsync(anotherTerminal.TerminalId, CancellationToken.None);
Check(await queue.CompleteAsync(anotherTerminal.TerminalId, failedJob.PrintJobId, false, "Paper out",
    CancellationToken.None), "Printer failure was not recorded.");
Check((await queue.QueueAsync(saved.OrderId, anotherTerminal.TerminalId, CancellationToken.None)).Status ==
    "Pending", "An explicit retry did not reset the failed job.");
Check((await queue.ClaimAsync(anotherTerminal.TerminalId, CancellationToken.None))?.Attempts == 2,
    "Print retry did not track attempts.");

var duplicateOrder = await orders.SaveAsync(orderForm, user.UserId, CancellationToken.None);
Check(duplicateOrder.Replayed && duplicateOrder.OrderId == saved.OrderId, "Retry created a second order.");
Check(await db.Orders.CountAsync() == 1 && await db.OrderItems.CountAsync() == 2,
    "Order retry changed persisted item count.");
Check(await db.OrderItemMeasurements.CountAsync() == 1,
    "Item-specific measurement snapshot is missing.");
Check(await db.OrderInvoiceSnapshots.AnyAsync(s =>
    s.OrderId == saved.OrderId && s.ReceiptFooter == "Thank you from TEST shop"),
    "Invoice branding and thank-you message snapshot is missing.");

var changedSettings = await db.ShopSettings.SingleAsync();
changedSettings.ReceiptFooter = "Later settings message";
await db.SaveChangesAsync();
Check(await db.OrderInvoiceSnapshots.AnyAsync(s =>
    s.OrderId == saved.OrderId && s.ReceiptFooter == "Thank you from TEST shop"),
    "Updating settings modified an existing invoice footer.");

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

var withoutRegister = await payments.PrepareAsync(saved.OrderId, user.UserId, CancellationToken.None);
await ExpectFailureAsync<InvalidOperationException>(() =>
    payments.SaveAsync(new PaymentForm
    {
        RequestId = withoutRegister.Form.RequestId,
        OrderId = saved.OrderId,
        Lines = [new PaymentLine { Method = "Cash", Amount = 1.000m }]
    }, user.UserId, CancellationToken.None), "Payment was accepted without the required open register.");

var register = new CashRegisterService(db);
var sessionId = await register.OpenAsync(5.000m, user.UserId, CancellationToken.None);
Check(sessionId > 0, "Cash register did not receive numeric identity.");
Check(await register.OpenAsync(9.000m, user.UserId, CancellationToken.None) == sessionId,
    "A second register was opened while one was already active.");

var expenseRequestCount = await db.CashExpenseRequests.CountAsync();
await register.GetPageAsync(CancellationToken.None);
await register.GetPageAsync(CancellationToken.None);
Check(await db.CashExpenseRequests.CountAsync() == expenseRequestCount,
    "Opening or refreshing the register page created expense requests.");

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
Check(secondPayment.PreviousPayments.Count == 2 &&
      secondPayment.PreviousPayments[0].Method == "Cash" &&
      secondPayment.PreviousPayments[0].Change == 2.000m &&
      secondPayment.PreviousPayments[1].Method == "KNET" &&
      secondPayment.Paid == 20.000m && secondPayment.Balance == 7.500m &&
      secondPayment.Refunded == 0m,
    "Returning customer did not see original invoice payment history and correct balance.");
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

Check(await db.OrderPayments.CountAsync(p => p.RegisterSessionId == sessionId) == 3 &&
      await db.OrderRefunds.CountAsync(r => r.RegisterSessionId == sessionId) == 1,
    "Payments or refunds were not linked to the open register.");

var expenseId = await register.ReserveExpenseAsync(sessionId, user.UserId, CancellationToken.None);
var expenseForm = new CashExpenseForm
{
    SessionId = sessionId,
    RequestId = expenseId,
    Amount = 1.000m,
    Reason = "Approved courier cash expense for test"
};
Check(!await register.AddExpenseAsync(expenseForm, user.UserId, CancellationToken.None),
    "Cash expense was not recorded.");
Check(await register.AddExpenseAsync(expenseForm, user.UserId, CancellationToken.None) &&
    await db.CashExpenses.CountAsync() == 1,
    "Cash expense retry created a duplicate expense.");

var closing = await register.CloseAsync(sessionId, 18.000m, user.UserId, CancellationToken.None);
Check(!closing.Replayed && closing.Expected == 17.500m && closing.Difference == 0.500m,
    "Cash register closing reconciliation was incorrect.");
Check((await register.CloseAsync(sessionId, 18.000m, user.UserId, CancellationToken.None)).Replayed,
    "Identical closing retry was not idempotent.");
await ExpectFailureAsync<InvalidOperationException>(() =>
    register.CloseAsync(sessionId, 18.500m, user.UserId, CancellationToken.None),
    "Different cash count was accepted after closing.");

await register.ApproveAsync(sessionId, user.UserId, CancellationToken.None);
Check(await db.CashRegisterSessions.AnyAsync(s =>
    s.SessionId == sessionId && s.ApprovedAtUtc.HasValue),
    "Manager approval did not persist.");

var postClosing = await payments.PrepareAsync(saved.OrderId, user.UserId, CancellationToken.None);
await ExpectFailureAsync<InvalidOperationException>(() =>
    payments.SaveAsync(new PaymentForm
    {
        RequestId = postClosing.Form.RequestId,
        OrderId = saved.OrderId,
        Lines = [new PaymentLine { Method = "Cash", Amount = 4.000m }]
    }, user.UserId, CancellationToken.None),
    "Payment was accepted after the required register was closed.");

var discountRequestId = await orders.ReserveAsync(user.UserId, CancellationToken.None);
var discountedForm = new OrderSaveForm
{
    RequestId = discountRequestId,
    CustomerId = customer.CustomerId,
    DeliveryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3).AddDays(1)),
    DiscountAmount = 1.000m,
    Items = [new OrderLineInput { ItemType = "Service", ItemId = service.ServiceId, Quantity = 1 }]
};
await ExpectFailureAsync<InvalidOperationException>(() =>
    orders.SaveAsync(discountedForm, user.UserId, CancellationToken.None),
    "POS discount was accepted while the setting was disabled.");
changedSettings.EnablePosDiscount = true;
await db.SaveChangesAsync();
var discounted = await orders.SaveAsync(discountedForm, user.UserId, CancellationToken.None);
Check(discounted.Total == 1.500m &&
    await db.Orders.AnyAsync(o => o.OrderId == discounted.OrderId &&
        o.Subtotal == 2.500m && o.DiscountAmount == 1.000m && o.Total == 1.500m),
    "Discounted order totals did not persist or reconcile.");
Check((await payments.PrepareAsync(discounted.OrderId, user.UserId, CancellationToken.None)).Balance == 1.500m,
    "Payment balance did not use the discounted total.");

Console.WriteLine("Domain integration passed: orders, discounts, payments, refunds, status history and cash register.");

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

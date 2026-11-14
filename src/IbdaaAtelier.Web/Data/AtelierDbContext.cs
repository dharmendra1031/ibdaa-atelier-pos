using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Orders;
using IbdaaAtelier.Web.Models.Payments;
using IbdaaAtelier.Web.Models.Registers;
using IbdaaAtelier.Web.Models.Printing;
using Microsoft.EntityFrameworkCore;

namespace IbdaaAtelier.Web.Data;

public sealed class AtelierDbContext(DbContextOptions<AtelierDbContext> options) : DbContext(options)
{
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductSize> ProductSizes => Set<ProductSize>();
    public DbSet<MeasurementDefinition> MeasurementDefinitions => Set<MeasurementDefinition>();
    public DbSet<SizeChartValue> SizeChartValues => Set<SizeChartValue>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ShopSettings> ShopSettings => Set<ShopSettings>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<AlterationService> AlterationServices => Set<AlterationService>();

    public DbSet<ShopUser> Users => Set<ShopUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<OrderRequest> OrderRequests => Set<OrderRequest>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderInvoiceSnapshot> OrderInvoiceSnapshots => Set<OrderInvoiceSnapshot>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemMeasurement> OrderItemMeasurements => Set<OrderItemMeasurement>();
    public DbSet<OrderStatusChange> OrderStatusHistory => Set<OrderStatusChange>();

    public DbSet<PaymentRequest> PaymentRequests => Set<PaymentRequest>();
    public DbSet<PaymentBatch> PaymentBatches => Set<PaymentBatch>();
    public DbSet<OrderPayment> OrderPayments => Set<OrderPayment>();
    public DbSet<CashRegisterSession> CashRegisterSessions => Set<CashRegisterSession>();
    public DbSet<CashExpenseRequest> CashExpenseRequests => Set<CashExpenseRequest>();
    public DbSet<CashExpense> CashExpenses => Set<CashExpense>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<OrderRefund> OrderRefunds => Set<OrderRefund>();
    public DbSet<PrintTerminal> PrintTerminals => Set<PrintTerminal>();
    public DbSet<PrintJob> PrintJobs => Set<PrintJob>();
}

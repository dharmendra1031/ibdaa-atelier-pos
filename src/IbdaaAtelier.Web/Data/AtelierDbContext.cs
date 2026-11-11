using IbdaaAtelier.Web.Models.Catalog;
using IbdaaAtelier.Web.Models.Security;
using IbdaaAtelier.Web.Models.Masters;
using IbdaaAtelier.Web.Models.Orders;
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
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<AlterationService> AlterationServices => Set<AlterationService>();

    public DbSet<ShopUser> Users => Set<ShopUser>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<OrderRequest> OrderRequests => Set<OrderRequest>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemMeasurement> OrderItemMeasurements => Set<OrderItemMeasurement>();
    public DbSet<OrderStatusChange> OrderStatusHistory => Set<OrderStatusChange>();
}

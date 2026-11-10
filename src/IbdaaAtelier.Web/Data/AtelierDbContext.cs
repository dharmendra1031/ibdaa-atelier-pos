using IbdaaAtelier.Web.Models.Catalog;
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
}


using Microsoft.EntityFrameworkCore;

#nullable disable

namespace SistemaVentas.Models;

public partial class SalesAnalyticsDBContext : DbContext
{
    public SalesAnalyticsDBContext(DbContextOptions<SalesAnalyticsDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<City> Cities { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new Configurations.CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.CityConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.CountryConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrderConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrderDetailConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.OrderStatusConfiguration());
        modelBuilder.ApplyConfiguration(new Configurations.ProductConfiguration());

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

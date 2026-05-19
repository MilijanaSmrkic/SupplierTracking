using Microsoft.EntityFrameworkCore;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User>           Users           => Set<User>();
    public DbSet<Supplier>       Suppliers       => Set<Supplier>();
    public DbSet<Product>        Products        => Set<Product>();
    public DbSet<Order>          Orders          => Set<Order>();
    public DbSet<OrderItem>      OrderItems      => Set<OrderItem>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

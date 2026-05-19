using Microsoft.EntityFrameworkCore;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User>            Users     { get; }
    DbSet<Supplier>        Suppliers { get; }
    DbSet<Product>         Products  { get; }
    DbSet<Order>           Orders    { get; }
    DbSet<OrderItem>       OrderItems   { get; }
    DbSet<OrderStatusLog>  OrderStatusLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

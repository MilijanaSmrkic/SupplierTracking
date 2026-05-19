using Microsoft.EntityFrameworkCore;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Domain.Entities;
using SupplierTracking.Infrastructure.Persistence;

namespace SupplierTracking.Infrastructure.Repositories;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context) => _context = context;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Orders
            .Include(o => o.Supplier)
            .Include(o => o.CreatedBy)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.StatusLogs.OrderByDescending(l => l.ChangedAt))
                .ThenInclude(l => l.ChangedBy)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default) =>
        _context.Orders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

    public Task<List<Order>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? supplierId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        // Safety caps — validator enforces these, but repository guards as last resort
        var safePage     = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.CreatedBy)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(o => o.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        return query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        Guid? supplierId = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(o => o.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        return query.CountAsync(cancellationToken);
    }

    public Task<List<Order>> GetOverdueAsync(CancellationToken cancellationToken = default) =>
        _context.Orders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.CreatedBy)
            .Where(o =>
                o.ExpectedDeliveryDate.HasValue &&
                o.ExpectedDeliveryDate.Value < DateTime.UtcNow &&
                o.Status != OrderStatuses.Delivered &&
                o.Status != OrderStatuses.Cancelled)
            .ToListAsync(cancellationToken);

    public void Add(Order order) => _context.Orders.Add(order);
}

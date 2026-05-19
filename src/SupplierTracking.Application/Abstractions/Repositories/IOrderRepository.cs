using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Abstractions.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<List<Order>> GetPagedAsync(
        int page,
        int pageSize,
        Guid? supplierId = null,
        string? status = null,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        Guid? supplierId = null,
        string? status = null,
        CancellationToken cancellationToken = default);
    Task<List<Order>> GetOverdueAsync(CancellationToken cancellationToken = default);
    void Add(Order order);
}

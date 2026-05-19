using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Abstractions.Repositories;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Supplier?> GetByWebhookSecretAsync(string secret, CancellationToken cancellationToken = default);
    Task<List<Supplier>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    void Add(Supplier supplier);
}

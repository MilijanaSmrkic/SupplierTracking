using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Abstractions.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Product>> GetBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default);
    void Add(Product product);
}

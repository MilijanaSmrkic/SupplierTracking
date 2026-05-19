using Microsoft.EntityFrameworkCore;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Domain.Entities;
using SupplierTracking.Infrastructure.Persistence;

namespace SupplierTracking.Infrastructure.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Products
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<List<Product>> GetBySupplierIdAsync(Guid supplierId, CancellationToken cancellationToken = default) =>
        _context.Products
            .Where(p => p.SupplierId == supplierId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default) =>
        _context.Products.AnyAsync(p => p.Sku == sku.ToUpperInvariant(), cancellationToken);

    public void Add(Product product) => _context.Products.Add(product);
}

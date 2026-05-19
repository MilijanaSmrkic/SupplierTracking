using Microsoft.EntityFrameworkCore;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Domain.Entities;
using SupplierTracking.Infrastructure.Persistence;

namespace SupplierTracking.Infrastructure.Repositories;

internal sealed class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _context;

    public SupplierRepository(ApplicationDbContext context) => _context = context;

    public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Supplier?> GetByWebhookSecretAsync(string secret, CancellationToken cancellationToken = default) =>
        _context.Suppliers
            .FirstOrDefaultAsync(s => s.WebhookSecret == secret && s.IsActive, cancellationToken);

    public Task<List<Supplier>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Suppliers.AnyAsync(s => s.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        _context.Suppliers.AnyAsync(s => s.Name == name, cancellationToken);

    public void Add(Supplier supplier) => _context.Suppliers.Add(supplier);
}

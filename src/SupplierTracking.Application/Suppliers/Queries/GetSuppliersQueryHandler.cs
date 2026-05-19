using MediatR;
using SupplierTracking.Application.Abstractions.Repositories;

namespace SupplierTracking.Application.Suppliers.Queries;

internal sealed class GetSuppliersQueryHandler
    : IRequestHandler<GetSuppliersQuery, List<SupplierResponse>>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSuppliersQueryHandler(ISupplierRepository supplierRepository) =>
        _supplierRepository = supplierRepository;

    public async Task<List<SupplierResponse>> Handle(
        GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var suppliers = await _supplierRepository.GetAllActiveAsync(cancellationToken);

        return suppliers.Select(s => new SupplierResponse(
            s.Id, s.Name, s.ContactEmail, s.ContactPhone, s.IsActive, s.CreatedAt))
            .ToList();
    }
}

internal sealed class GetSupplierByIdQueryHandler
    : IRequestHandler<GetSupplierByIdQuery, SupplierResponse>
{
    private readonly ISupplierRepository _supplierRepository;

    public GetSupplierByIdQueryHandler(ISupplierRepository supplierRepository) =>
        _supplierRepository = supplierRepository;

    public async Task<SupplierResponse> Handle(
        GetSupplierByIdQuery request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier with id {request.Id} was not found.");

        return new SupplierResponse(
            supplier.Id, supplier.Name, supplier.ContactEmail,
            supplier.ContactPhone, supplier.IsActive, supplier.CreatedAt);
    }
}

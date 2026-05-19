using MediatR;

namespace SupplierTracking.Application.Suppliers.Queries;

public record SupplierResponse(
    Guid    Id,
    string  Name,
    string  ContactEmail,
    string? ContactPhone,
    bool    IsActive,
    DateTime CreatedAt);

public record GetSuppliersQuery : IRequest<List<SupplierResponse>>;

public record GetSupplierByIdQuery(Guid Id) : IRequest<SupplierResponse>;

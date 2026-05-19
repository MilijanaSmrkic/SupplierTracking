using MediatR;
using Microsoft.Extensions.Logging;

namespace SupplierTracking.Application.Suppliers.Commands;

public record CreateSupplierCommand(
    string  Name,
    string  ContactEmail,
    string? ContactPhone = null) : IRequest<Guid>;

internal sealed class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, Guid>
{
    private readonly Abstractions.Repositories.ISupplierRepository _supplierRepository;
    private readonly Abstractions.IUnitOfWork _unitOfWork;
    private readonly Microsoft.Extensions.Logging.ILogger<CreateSupplierCommandHandler> _logger;

    public CreateSupplierCommandHandler(
        Abstractions.Repositories.ISupplierRepository supplierRepository,
        Abstractions.IUnitOfWork unitOfWork,
        Microsoft.Extensions.Logging.ILogger<CreateSupplierCommandHandler> logger)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork         = unitOfWork;
        _logger             = logger;
    }

    public async Task<Guid> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        if (await _supplierRepository.NameExistsAsync(request.Name, cancellationToken))
            throw new InvalidOperationException($"Supplier with name '{request.Name}' already exists.");

        var supplier = Domain.Entities.Supplier.Create(
            request.Name, request.ContactEmail, request.ContactPhone);

        _supplierRepository.Add(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Supplier '{Name}' created (Id={SupplierId})", supplier.Name, supplier.Id);

        return supplier.Id;
    }
}

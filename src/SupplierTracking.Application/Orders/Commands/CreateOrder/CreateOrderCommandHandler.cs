using MediatR;
using Microsoft.Extensions.Logging;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Orders.Commands.CreateOrder;

internal sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository       _orderRepository;
    private readonly ISupplierRepository    _supplierRepository;
    private readonly IProductRepository     _productRepository;
    private readonly IUnitOfWork            _unitOfWork;
    private readonly ICurrentUserService    _currentUser;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ISupplierRepository supplierRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository    = orderRepository;
        _supplierRepository = supplierRepository;
        _productRepository  = productRepository;
        _unitOfWork         = unitOfWork;
        _currentUser        = currentUser;
        _logger             = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
            throw new KeyNotFoundException($"Active supplier with id {request.SupplierId} was not found.");

        var order = Order.Create(
            request.SupplierId,
            _currentUser.UserId,
            request.ExpectedDeliveryDate,
            request.Notes);

        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
                throw new KeyNotFoundException($"Active product with id {item.ProductId} was not found.");

            if (product.SupplierId != request.SupplierId)
                throw new InvalidOperationException(
                    $"Product '{product.Name}' does not belong to supplier '{supplier.Name}'.");

            order.AddItem(product.Id, item.Quantity, product.UnitPrice);
        }

        _orderRepository.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} created (Id={OrderId}) — supplier '{SupplierName}', {ItemCount} items, total {Total:C}, by user {UserId}",
            order.OrderNumber, order.Id, supplier.Name,
            order.Items.Count, order.TotalAmount, _currentUser.UserId);

        return order.Id;
    }
}

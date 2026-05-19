using MediatR;

namespace SupplierTracking.Application.Orders.Commands.CreateOrder;

public record OrderItemRequest(Guid ProductId, int Quantity);

public record CreateOrderCommand(
    Guid                   SupplierId,
    List<OrderItemRequest> Items,
    DateTime?              ExpectedDeliveryDate = null,
    string?                Notes = null) : IRequest<Guid>;

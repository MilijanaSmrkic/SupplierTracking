using MediatR;

namespace SupplierTracking.Application.Orders.Commands.SendOrder;

public record SendOrderCommand(Guid OrderId) : IRequest;

using MediatR;

namespace SupplierTracking.Application.Orders.Commands.ConfirmOrder;

public record ConfirmOrderCommand(Guid OrderId, string? Notes = null) : IRequest;

using MediatR;

namespace SupplierTracking.Application.Orders.Commands.MarkDelivered;

public record MarkDeliveredCommand(Guid OrderId) : IRequest;

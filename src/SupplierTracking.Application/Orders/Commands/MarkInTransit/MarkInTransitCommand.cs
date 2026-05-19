using MediatR;

namespace SupplierTracking.Application.Orders.Commands.MarkInTransit;

public record MarkInTransitCommand(Guid OrderId, string? TrackingCode = null) : IRequest;

using MediatR;

namespace SupplierTracking.Application.Orders.Events;

public record OrderStatusChangedEvent(
    Guid   OrderId,
    string OrderNumber,
    Guid   SupplierId,
    string SupplierName,
    string FromStatus,
    string ToStatus,
    int?   ChangedByUserId,
    DateTime ChangedAt) : INotification;

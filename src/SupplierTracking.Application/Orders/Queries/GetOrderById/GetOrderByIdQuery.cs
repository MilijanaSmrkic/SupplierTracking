using MediatR;

namespace SupplierTracking.Application.Orders.Queries.GetOrderById;

public record OrderItemResponse(
    Guid    ProductId,
    string  ProductName,
    string  Sku,
    int     Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public record StatusLogResponse(
    string?  FromStatus,
    string   ToStatus,
    string?  ChangedBy,
    DateTime ChangedAt,
    string?  Notes);

public record OrderDetailResponse(
    Guid                    Id,
    string                  OrderNumber,
    Guid                    SupplierId,
    string                  SupplierName,
    string                  CreatedByUserName,
    string                  Status,
    string?                 TrackingCode,
    string?                 Notes,
    DateTime?               ExpectedDeliveryDate,
    bool                    IsOverdue,
    decimal                 TotalAmount,
    DateTime                CreatedAt,
    DateTime                UpdatedAt,
    List<OrderItemResponse> Items,
    List<StatusLogResponse> StatusLogs);

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailResponse>;

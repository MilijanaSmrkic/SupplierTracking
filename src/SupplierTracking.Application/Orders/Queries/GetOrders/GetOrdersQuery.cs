using MediatR;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Orders.Queries.GetOrders;

public record OrderSummaryResponse(
    Guid      Id,
    string    OrderNumber,
    Guid      SupplierId,
    string    SupplierName,
    string    Status,
    int       ItemCount,
    decimal   TotalAmount,
    DateTime? ExpectedDeliveryDate,
    bool      IsOverdue,
    DateTime  CreatedAt);

public record GetOrdersQuery(
    int    Page       = 1,
    int    PageSize   = 20,
    Guid?  SupplierId = null,
    string? Status    = null) : IRequest<PagedResult<OrderSummaryResponse>>;

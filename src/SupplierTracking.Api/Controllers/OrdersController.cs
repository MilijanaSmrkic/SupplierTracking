using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierTracking.Application.Orders.Commands.CancelOrder;
using SupplierTracking.Application.Orders.Commands.ConfirmOrder;
using SupplierTracking.Application.Orders.Commands.CreateOrder;
using SupplierTracking.Application.Orders.Commands.MarkDelivered;
using SupplierTracking.Application.Orders.Commands.MarkInTransit;
using SupplierTracking.Application.Orders.Commands.SendOrder;
using SupplierTracking.Application.Orders.Queries.GetOrderById;
using SupplierTracking.Application.Orders.Queries.GetOrders;
using SupplierTracking.Application.Models;
using SupplierTracking.Domain.Entities;
// ReSharper disable once RedundantUsingDirective (used in ProducesResponseType attributes)

namespace SupplierTracking.Api.Controllers;

/// <summary>
/// Manage supplier orders and drive them through the status lifecycle:
/// Draft → Sent → Confirmed → InTransit → Delivered (or Cancelled at any stage before delivery).
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <inheritdoc />
    public OrdersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get a paginated list of orders, optionally filtered by supplier and/or status.
    /// </summary>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, max 100 (default: 20).</param>
    /// <param name="supplierId">Filter by supplier GUID.</param>
    /// <param name="status">Filter by status: Draft | Sent | Confirmed | InTransit | Delivered | Cancelled.</param>
    /// <response code="200">Paged list of orders.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int    page       = 1,
        [FromQuery] int    pageSize   = 20,
        [FromQuery] Guid?  supplierId = null,
        [FromQuery] string? status    = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetOrdersQuery(page, pageSize, supplierId, status),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get full order details including items and status log.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <response code="200">Order detail.</response>
    /// <response code="404">Order not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new order in Draft status. Requires Admin or Manager role.
    /// </summary>
    /// <response code="201">Order created. Location header points to the new resource.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>
    /// Send a Draft order to the supplier. Transitions: Draft → Sent.
    /// The order must have at least one item. Requires Admin or Manager role.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <response code="204">Order sent.</response>
    /// <response code="400">Order has no items or is not in Draft status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SendOrderCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Confirm a Sent order. Transitions: Sent → Confirmed.
    /// Requires Admin or Manager role.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <param name="request">Optional notes.</param>
    /// <response code="204">Order confirmed.</response>
    /// <response code="400">Order is not in Sent status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(
        Guid id,
        [FromBody] ConfirmRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ConfirmOrderCommand(id, request.Notes), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Mark a Confirmed order as In Transit. Transitions: Confirmed → InTransit.
    /// Optionally attach a courier tracking code. Requires Admin or Manager role.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <param name="request">Optional tracking code (e.g. DHL-123456).</param>
    /// <response code="204">Order is now in transit.</response>
    /// <response code="400">Order is not in Confirmed status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/in-transit")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkInTransit(
        Guid id,
        [FromBody] InTransitRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkInTransitCommand(id, request.TrackingCode), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Mark an In Transit order as Delivered. Transitions: InTransit → Delivered.
    /// Requires Admin or Manager role.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <response code="204">Order delivered.</response>
    /// <response code="400">Order is not in InTransit status.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/deliver")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkDeliveredCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cancel an order. Allowed from Draft, Sent, and Confirmed statuses.
    /// Managers and Admins can cancel any order; a Viewer can only cancel their own.
    /// </summary>
    /// <param name="id">Order GUID.</param>
    /// <param name="request">Optional cancellation reason.</param>
    /// <response code="204">Order cancelled.</response>
    /// <response code="400">Order is already Delivered or Cancelled.</response>
    /// <response code="403">Not authorized to cancel this order.</response>
    /// <response code="404">Order not found.</response>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelOrderCommand(id, request.Reason), cancellationToken);
        return NoContent();
    }
}

/// <summary>Notes for order confirmation.</summary>
public record ConfirmRequest(string? Notes = null);

/// <summary>Tracking code for in-transit update.</summary>
public record InTransitRequest(string? TrackingCode = null);

/// <summary>Reason for cancellation.</summary>
public record CancelRequest(string? Reason = null);

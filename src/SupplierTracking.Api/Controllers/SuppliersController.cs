using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplierTracking.Application.Suppliers.Commands;
using SupplierTracking.Application.Suppliers.Queries;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Api.Controllers;

/// <summary>
/// Manage suppliers.
/// </summary>
[ApiController]
[Route("api/suppliers")]
[Authorize]
[Produces("application/json")]
public sealed class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <inheritdoc />
    public SuppliersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all active suppliers.
    /// </summary>
    /// <response code="200">List of suppliers.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSuppliersQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a supplier by ID.
    /// </summary>
    /// <param name="id">Supplier GUID.</param>
    /// <response code="200">Supplier details.</response>
    /// <response code="404">Supplier not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSupplierByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new supplier. Requires Admin or Manager role.
    /// </summary>
    /// <remarks>
    /// A unique webhook secret is automatically generated for each supplier.
    /// Use <c>GET /api/suppliers/{id}</c> to retrieve it and share it with the supplier
    /// so they can sign their webhook calls.
    /// </remarks>
    /// <response code="201">Supplier created. Returns the new supplier ID.</response>
    /// <response code="400">Validation error or name already exists.</response>
    /// <response code="403">Insufficient role.</response>
    [HttpPost]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Manager}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}

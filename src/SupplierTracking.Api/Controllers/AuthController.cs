using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupplierTracking.Application.Auth;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Api.Controllers;

/// <summary>
/// Authentication — obtain a JWT Bearer token.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <inheritdoc />
    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Login with username and password and receive a JWT token.
    /// </summary>
    /// <remarks>
    /// The default seeded admin credentials are:
    ///
    ///     POST /api/auth/login
    ///     {
    ///         "username": "admin",
    ///         "password": "Admin123!"
    ///     }
    ///
    /// Pass the returned `token` as `Bearer {token}` in the `Authorization` header for all subsequent requests.
    /// </remarks>
    /// <response code="200">Returns the JWT token and its expiry.</response>
    /// <response code="401">Invalid credentials.</response>
    /// <response code="429">Too many attempts — wait 1 minute before retrying.</response>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

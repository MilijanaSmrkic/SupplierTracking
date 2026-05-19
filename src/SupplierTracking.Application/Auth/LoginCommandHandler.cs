using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Auth;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService         _tokenService;
    private readonly JwtSettings           _jwtSettings;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<LoginCommandHandler> logger)
    {
        _context      = context;
        _tokenService = tokenService;
        _jwtSettings  = jwtSettings.Value;
        _logger       = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.IsActive, cancellationToken);

        if (user is null || !_tokenService.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for username '{UserName}'", request.UserName);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var token = _tokenService.GenerateToken(user);

        _logger.LogInformation(
            "User '{UserName}' (Id={UserId}, Role={Role}) logged in successfully",
            user.UserName, user.Id, user.Role);

        return new LoginResponse(
            token,
            user.UserName,
            user.Role,
            DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes));
    }
}

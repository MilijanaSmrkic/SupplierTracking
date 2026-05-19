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
    private readonly ITokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<LoginCommandHandler> _logger;

    private const int RefreshTokenExpiryDays = 1;

    public LoginCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<LoginCommandHandler> logger)
    {
        _context = context;
        _tokenService = tokenService;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
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

        // Generate tokens
        var accessToken = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshExpiry = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        // Store hashed refresh token — plain text never persisted
        user.RefreshTokenHash = _tokenService.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiry = refreshExpiry;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User '{UserName}' (Id={UserId}, Role={Role}) logged in successfully",
            user.UserName, user.Id, user.Role);

        return new LoginResponse(
            Token: accessToken,
            RefreshToken: refreshToken,
            UserName: user.UserName,
            Role: user.Role,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            RefreshTokenExpiresAt: refreshExpiry);
    }
}

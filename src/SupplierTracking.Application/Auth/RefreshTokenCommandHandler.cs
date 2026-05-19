using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Auth;

internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService         _tokenService;
    private readonly JwtSettings           _jwtSettings;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    private const int RefreshTokenExpiryDays = 7;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IOptions<JwtSettings> jwtSettings,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _context      = context;
        _tokenService = tokenService;
        _jwtSettings  = jwtSettings.Value;
        _logger       = logger;
    }

    public async Task<LoginResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find any active user that has a stored refresh token hash
        // We can't query by the plain token — we must check all candidates or use a lookup index.
        // Since refresh tokens are per-user (1 active at a time), we load candidates
        // with non-expired tokens and verify with constant-time comparison.
        var candidates = await _context.Users
            .Where(u =>
                u.IsActive &&
                u.RefreshTokenHash != null &&
                u.RefreshTokenExpiry > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        var user = candidates.FirstOrDefault(u =>
            _tokenService.VerifyRefreshToken(request.RefreshToken, u.RefreshTokenHash!));

        if (user is null)
        {
            _logger.LogWarning("Invalid or expired refresh token attempt");
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Rotate — invalidate old token, issue new pair
        var newAccessToken  = _tokenService.GenerateToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var refreshExpiry   = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays);

        user.RefreshTokenHash   = _tokenService.HashRefreshToken(newRefreshToken);
        user.RefreshTokenExpiry = refreshExpiry;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Refresh token rotated for user '{UserName}' (Id={UserId})", user.UserName, user.Id);

        return new LoginResponse(
            Token:                 newAccessToken,
            RefreshToken:          newRefreshToken,
            UserName:              user.UserName,
            Role:                  user.Role,
            ExpiresAt:             DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
            RefreshTokenExpiresAt: refreshExpiry);
    }
}

using MediatR;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Auth;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResponse>;

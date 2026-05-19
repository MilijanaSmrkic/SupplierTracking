using MediatR;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Auth;

public record LoginCommand(string UserName, string Password) : IRequest<LoginResponse>;

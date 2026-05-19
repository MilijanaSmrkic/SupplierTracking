namespace SupplierTracking.Application.Abstractions;

public interface ICurrentUserService
{
    int    UserId    { get; }
    string Role      { get; }
    bool   IsAdmin   => Role == Domain.Entities.UserRoles.Admin;
    bool   IsManager => Role == Domain.Entities.UserRoles.Manager || IsAdmin;
}

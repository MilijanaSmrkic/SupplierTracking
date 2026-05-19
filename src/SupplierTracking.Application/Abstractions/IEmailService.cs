using SupplierTracking.Application.Models;

namespace SupplierTracking.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

namespace SupplierTracking.Application.Jobs;

public interface IDailyDigestJob
{
    Task SendDailyDigestAsync(CancellationToken cancellationToken = default);
}

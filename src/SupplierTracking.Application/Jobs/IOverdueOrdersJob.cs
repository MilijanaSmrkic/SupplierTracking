namespace SupplierTracking.Application.Jobs;

public interface IOverdueOrdersJob
{
    Task CheckOverdueOrdersAsync(CancellationToken cancellationToken = default);
}

using Hangfire;
using SupplierTracking.Application.Jobs;

namespace SupplierTracking.Infrastructure.Jobs;

public static class HangfireJobRegistrar
{
    public static void RegisterRecurringJobs()
    {
        // Check for overdue orders every 6 hours
        RecurringJob.AddOrUpdate<IOverdueOrdersJob>(
            recurringJobId: "overdue-orders-check",
            methodCall:     job => job.CheckOverdueOrdersAsync(CancellationToken.None),
            cronExpression: "0 */6 * * *");

        // Send daily digest every day at 8:00 AM UTC
        RecurringJob.AddOrUpdate<IDailyDigestJob>(
            recurringJobId: "daily-digest",
            methodCall:     job => job.SendDailyDigestAsync(CancellationToken.None),
            cronExpression: "0 8 * * *");
    }
}

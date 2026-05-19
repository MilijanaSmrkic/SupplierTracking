using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Jobs;
using SupplierTracking.Application.Models;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Infrastructure.Jobs;

internal sealed class DailyDigestJob : IDailyDigestJob
{
    private readonly IOrderRepository  _orderRepository;
    private readonly IEmailService     _emailService;
    private readonly SmtpSettings      _smtpSettings;
    private readonly ILogger<DailyDigestJob> _logger;

    private static readonly string[] ActiveStatuses =
        [OrderStatuses.Draft, OrderStatuses.Sent, OrderStatuses.Confirmed, OrderStatuses.InTransit];

    public DailyDigestJob(
        IOrderRepository orderRepository,
        IEmailService emailService,
        IOptions<SmtpSettings> smtpSettings,
        ILogger<DailyDigestJob> logger)
    {
        _orderRepository = orderRepository;
        _emailService    = emailService;
        _smtpSettings    = smtpSettings.Value;
        _logger          = logger;
    }

    public async Task SendDailyDigestAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_smtpSettings.DigestRecipient))
        {
            _logger.LogDebug("Daily digest skipped — DigestRecipient is not configured");
            return;
        }

        // Fetch active orders for each status
        var allActive = new List<(string Status, int Count, List<string> Orders)>();

        foreach (var status in ActiveStatuses)
        {
            var orders = await _orderRepository.GetPagedAsync(
                page: 1, pageSize: 100, status: status,
                cancellationToken: cancellationToken);

            if (orders.Count > 0)
                allActive.Add((status, orders.Count, orders.Select(o => o.OrderNumber).ToList()));
        }

        var overdueOrders = await _orderRepository.GetOverdueAsync(cancellationToken);
        var totalActive   = allActive.Sum(x => x.Count);

        _logger.LogInformation(
            "Daily digest prepared — {TotalActive} active orders, {OverdueCount} overdue",
            totalActive, overdueOrders.Count);

        var body = BuildDigestBody(allActive, overdueOrders);

        try
        {
            await _emailService.SendAsync(new EmailMessage(
                To:      _smtpSettings.DigestRecipient,
                Subject: $"[Daily Digest] Order Summary — {DateTime.UtcNow:yyyy-MM-dd}",
                Body:    body), cancellationToken);

            _logger.LogInformation("Daily digest email sent to {Recipient}", _smtpSettings.DigestRecipient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send daily digest email to {Recipient}", _smtpSettings.DigestRecipient);
            // Do not rethrow — a failed digest email should not mark the Hangfire job as failed
            // so it won't spam retries. The issue will be visible in logs and Hangfire dashboard.
        }
    }

    private static string BuildDigestBody(
        List<(string Status, int Count, List<string> Orders)> activeByStatus,
        List<Domain.Entities.Order> overdueOrders)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"DAILY ORDER DIGEST — {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();

        if (activeByStatus.Count == 0)
        {
            sb.AppendLine("No active orders at this time.");
        }
        else
        {
            sb.AppendLine("ACTIVE ORDERS BY STATUS:");
            sb.AppendLine();
            foreach (var (status, count, orders) in activeByStatus)
            {
                sb.AppendLine($"  {status}: {count} order(s)");
                foreach (var orderNumber in orders.Take(5))
                    sb.AppendLine($"    - {orderNumber}");
                if (orders.Count > 5)
                    sb.AppendLine($"    ... and {orders.Count - 5} more");
                sb.AppendLine();
            }
        }

        if (overdueOrders.Count > 0)
        {
            sb.AppendLine(new string('-', 50));
            sb.AppendLine($"⚠ OVERDUE ORDERS ({overdueOrders.Count}):");
            sb.AppendLine();
            foreach (var order in overdueOrders)
            {
                var daysOverdue = (int)(DateTime.UtcNow - order.ExpectedDeliveryDate!.Value).TotalDays;
                sb.AppendLine($"  - {order.OrderNumber} | {order.Supplier?.Name} | {daysOverdue} day(s) overdue | Status: {order.Status}");
            }
        }

        return sb.ToString();
    }
}

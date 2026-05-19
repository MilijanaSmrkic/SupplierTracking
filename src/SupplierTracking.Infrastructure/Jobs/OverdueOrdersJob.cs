using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Jobs;
using SupplierTracking.Application.Models;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Infrastructure.Jobs;

internal sealed class OverdueOrdersJob : IOverdueOrdersJob
{
    private readonly IOrderRepository  _orderRepository;
    private readonly IEmailService     _emailService;
    private readonly SmtpSettings      _smtpSettings;
    private readonly ILogger<OverdueOrdersJob> _logger;

    // Orders overdue by more than this many days trigger escalation email
    private const int EscalationThresholdDays = 3;

    public OverdueOrdersJob(
        IOrderRepository orderRepository,
        IEmailService emailService,
        IOptions<SmtpSettings> smtpSettings,
        ILogger<OverdueOrdersJob> logger)
    {
        _orderRepository = orderRepository;
        _emailService    = emailService;
        _smtpSettings    = smtpSettings.Value;
        _logger          = logger;
    }

    public async Task CheckOverdueOrdersAsync(CancellationToken cancellationToken = default)
    {
        var overdueOrders = await _orderRepository.GetOverdueAsync(cancellationToken);

        if (overdueOrders.Count == 0)
        {
            _logger.LogDebug("Overdue check completed — no overdue orders found");
            return;
        }

        _logger.LogWarning(
            "Overdue check found {Count} overdue order(s)", overdueOrders.Count);

        var failed = 0;

        foreach (var order in overdueOrders)
        {
            try
            {
                var daysOverdue = (int)(DateTime.UtcNow - order.ExpectedDeliveryDate!.Value).TotalDays;

                _logger.LogWarning(
                    "Order {OrderNumber} is {DaysOverdue} day(s) overdue — status: {Status}, supplier: '{SupplierName}'",
                    order.OrderNumber, daysOverdue, order.Status, order.Supplier?.Name);

                // Escalation: orders overdue more than threshold go to manager
                if (daysOverdue >= EscalationThresholdDays && !string.IsNullOrWhiteSpace(_smtpSettings.DigestRecipient))
                {
                    await SendEscalationEmailAsync(order, daysOverdue, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "Failed to process overdue escalation for order {OrderNumber}", order.OrderNumber);
                // Continue processing remaining orders even if one fails
            }
        }

        if (failed > 0)
            _logger.LogWarning(
                "Overdue check completed with {Failed} error(s) out of {Total} order(s)",
                failed, overdueOrders.Count);
    }

    private Task SendEscalationEmailAsync(Order order, int daysOverdue, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Escalating order {OrderNumber} — {DaysOverdue} days overdue",
            order.OrderNumber, daysOverdue);

        var body =
            $"ESCALATION ALERT\n\n" +
            $"Order {order.OrderNumber} from supplier '{order.Supplier?.Name}' " +
            $"is {daysOverdue} day(s) overdue.\n\n" +
            $"Current status: {order.Status}\n" +
            $"Expected delivery: {order.ExpectedDeliveryDate:yyyy-MM-dd}\n\n" +
            $"Please take immediate action.";

        return _emailService.SendAsync(new EmailMessage(
            To:      _smtpSettings.DigestRecipient,
            Subject: $"[ESCALATION] Order {order.OrderNumber} is {daysOverdue} days overdue",
            Body:    body), cancellationToken);
    }
}

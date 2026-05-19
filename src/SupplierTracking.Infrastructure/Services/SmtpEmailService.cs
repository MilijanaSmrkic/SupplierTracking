using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Models;

namespace SupplierTracking.Infrastructure.Services;

internal sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    private const int MaxRetries = 3;

    public SmtpEmailService(IOptions<SmtpSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger   = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl   = _settings.UseSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };

                var mail = new MailMessage(_settings.From, message.To, message.Subject, message.Body);
                await client.SendMailAsync(mail, cancellationToken);

                _logger.LogInformation(
                    "Email sent to {To} — subject: '{Subject}'",
                    message.To, message.Subject);

                return;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    "Email attempt {Attempt}/{Max} failed for {To}. Retrying in {Delay}s. Error: {Error}",
                    attempt, MaxRetries, message.To, delay.TotalSeconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send email to {To} after {Max} attempts — subject: '{Subject}'",
                    message.To, MaxRetries, message.Subject);
            }
        }
    }
}

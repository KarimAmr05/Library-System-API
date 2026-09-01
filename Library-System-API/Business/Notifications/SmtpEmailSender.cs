using System.Net;
using System.Net.Mail;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Shared.Configuration;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Business.Notifications;

/// <summary>
/// SMTP-backed email sender. When no SMTP host is configured (local
/// development) the message content is logged instead so flows that send
/// mail remain fully testable without infrastructure.
/// Delivery failures are logged and swallowed: callers treat email as
/// best-effort and must not fail business operations on it.
/// </summary>
/// <param name="smtpSettings">SMTP configuration.</param>
/// <param name="logger">Structured logger.</param>
public sealed class SmtpEmailSender(
    IOptions<SmtpSettings> smtpSettings,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _smtp = smtpSettings?.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
    private readonly ILogger<SmtpEmailSender> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            // Dev fallback: surface the full content so manual testing can proceed.
            _logger.LogInformation(
                "Email (SMTP disabled) — To: {To} | Subject: {Subject}\n{Body}",
                to, subject, body);
            return;
        }

        try
        {
            using var message = new MailMessage(_smtp.From, to, subject, body);
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_smtp.UserName, _smtp.Password)
            };

            await client.SendMailAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {To}. Continuing without failing the request.", to);
        }
    }
}

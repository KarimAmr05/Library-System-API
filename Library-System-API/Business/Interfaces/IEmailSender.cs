namespace LibrarySystem.Business.Interfaces;

/// <summary>
/// Abstraction over outgoing email delivery.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a plain-text email. Implementations must not throw for delivery
    /// hiccups that should not fail the surrounding business operation —
    /// they log instead.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Plain-text body.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

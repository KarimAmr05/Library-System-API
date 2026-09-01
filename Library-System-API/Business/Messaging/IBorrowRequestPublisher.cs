namespace LibrarySystem.Business.Messaging;

/// <summary>
/// Publishes borrowing-request messages to RabbitMQ. The API remains responsible
/// only for accepting requests; all queue plumbing lives behind this abstraction.
/// </summary>
public interface IBorrowRequestPublisher
{
    /// <summary>
    /// Publishes a borrowing-request message to the configured durable queue.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task PublishAsync(BorrowRequestMessage message, CancellationToken cancellationToken = default);
}

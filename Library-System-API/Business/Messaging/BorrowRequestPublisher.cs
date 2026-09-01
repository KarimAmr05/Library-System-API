using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace LibrarySystem.Business.Messaging;

/// <summary>
/// Default publisher writing JSON-serialized borrow-request messages to the
/// durable queue with persistent delivery mode.
/// </summary>
/// <param name="connection">Shared broker connection.</param>
/// <param name="logger">Structured logger.</param>
public sealed class BorrowRequestPublisher(
    RabbitMqConnection connection,
    ILogger<BorrowRequestPublisher> logger) : IBorrowRequestPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly ILogger<BorrowRequestPublisher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task PublishAsync(BorrowRequestMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var channel = await _connection.GetChannelAsync(cancellationToken).ConfigureAwait(false);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, SerializerOptions));

        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = message.RequestId.ToString(),
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _connection.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Published borrow request message for request {RequestId}.",
            message.RequestId);
    }
}

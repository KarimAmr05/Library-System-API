using System.Text;
using System.Text.Json;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LibrarySystem.Business.Messaging;

/// <summary>
/// Hosted service consuming borrowing-request messages from RabbitMQ.
/// Messages are acknowledged only after the borrowing workflow completes
/// successfully; transient failures are requeued once, then rejected to avoid
/// poison-message loops. Reconnects with a delay when the broker is unavailable.
/// </summary>
/// <param name="connection">Shared broker connection.</param>
/// <param name="serviceScopeFactory">Scope factory resolving scoped business services per message.</param>
/// <param name="logger">Structured logger.</param>
public sealed class BorrowRequestConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<BorrowRequestConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RabbitMqConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly IServiceScopeFactory _serviceScopeFactory =
        serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly ILogger<BorrowRequestConsumer> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var channel = await _connection.GetChannelAsync(stoppingToken).ConfigureAwait(false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, eventArgs) =>
                    HandleMessageAsync(channel, eventArgs, stoppingToken);

                // Manual acknowledgement: nothing is acked before successful processing.
                await channel.BasicConsumeAsync(
                    queue: _connection.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "BorrowRequestConsumer listening on queue {Queue}.", _connection.QueueName);

                // Keep consuming until shutdown; BasicConsumeAsync returns immediately.
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BorrowRequestConsumer failure. Retrying in 10 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("BorrowRequestConsumer stopped.");
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var deliveryTag = eventArgs.DeliveryTag;

        BorrowRequestMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<BorrowRequestMessage>(
                Encoding.UTF8.GetString(eventArgs.Body.Span), SerializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Undecodable borrow request message received. Rejecting permanently.");
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        if (message is null || message.RequestId == Guid.Empty)
        {
            _logger.LogError("Invalid borrow request message payload. Rejecting permanently.");
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false, cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var borrowingService = scope.ServiceProvider.GetRequiredService<IBorrowingService>();
            await borrowingService.ProcessQueuedRequestAsync(message.RequestId, cancellationToken).ConfigureAwait(false);

            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Processed and acked borrow request {RequestId}.", message.RequestId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown mid-processing: leave unacknowledged so the broker redelivers later.
            _logger.LogInformation("Shutdown during processing of {RequestId}; leaving unacked.", message.RequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed for borrow request {RequestId}.", message.RequestId);

            // Requeue on first attempt so a transient failure can succeed;
            // reject permanently on redelivery to avoid infinite loops.
            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: !eventArgs.Redelivered, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}

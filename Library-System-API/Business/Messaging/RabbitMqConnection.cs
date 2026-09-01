using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LibrarySystem.Business.Messaging;

/// <summary>
/// Owns the singleton RabbitMQ connection shared by publisher and consumer,
/// with lazy creation and automatic reconnection handling by the client library.
/// </summary>
/// <param name="settings">Broker configuration.</param>
/// <param name="logger">Structured logger.</param>
public sealed class RabbitMqConnection(
    IOptions<RabbitMqSettings> settings,
    ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{
    private readonly RabbitMqSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<RabbitMqConnection> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    /// <summary>
    /// Gets a connected channel with the borrowing queue declared, creating
    /// connection/channel on first use or after a lost connection.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An open channel with the durable queue declared.</returns>
    public async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return _channel;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return _channel!;
            }

            await DisposeResourcesAsync().ConfigureAwait(false);

            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            _logger.LogInformation("Connecting to RabbitMQ at {HostName}:{Port}.", _settings.HostName, _settings.Port);
            _connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            // Durable queue survives broker restarts; single consumer workflow.
            await _channel.QueueDeclareAsync(
                queue: _settings.BorrowRequestQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets the configured borrowing queue name.
    /// </summary>
    public string QueueName => _settings.BorrowRequestQueue;

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await DisposeResourcesAsync().ConfigureAwait(false);

    private async Task DisposeResourcesAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ channel.");
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ connection.");
            }

            _connection = null;
        }
    }
}

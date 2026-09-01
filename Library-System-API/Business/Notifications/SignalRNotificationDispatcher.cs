using LibrarySystem.Business.DTOs.Notifications;
using LibrarySystem.Business.Hubs;
using LibrarySystem.Business.Mappings;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Business.Notifications;

/// <summary>
/// Default dispatcher delivering notifications through SignalR user/role groups.
/// Delivery failures are logged and swallowed: persistence already succeeded,
/// so a transient push failure must not fail the surrounding business operation.
/// </summary>
/// <param name="hubContext">SignalR hub context for the notifications hub.</param>
/// <param name="logger">Structured logger.</param>
public sealed class SignalRNotificationDispatcher(
    IHubContext<NotificationsHub> hubContext,
    ILogger<SignalRNotificationDispatcher> logger) : INotificationDispatcher
{
    private readonly IHubContext<NotificationsHub> _hubContext =
        hubContext ?? throw new ArgumentNullException(nameof(hubContext));

    private readonly ILogger<SignalRNotificationDispatcher> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        try
        {
            var dto = notification.ToDto();

            if (notification.RecipientRole == UserRole.Admin)
            {
                // Deliver to every connected admin; individual admin ids also receive
                // their own copy via their user group.
                await _hubContext.Clients.Group(NotificationsHub.AdminsGroup)
                    .SendAsync("notificationReceived", dto, cancellationToken).ConfigureAwait(false);
            }

            await _hubContext.Clients.Group(NotificationsHub.UserGroup(notification.RecipientUserId))
                .SendAsync("notificationReceived", dto, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Real-time dispatch failed for notification {NotificationId} to user {RecipientUserId}.",
                notification.Id, notification.RecipientUserId);
        }
    }
}

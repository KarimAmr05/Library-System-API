using LibrarySystem.DataAccess.Entities;

namespace LibrarySystem.Business.Notifications;

/// <summary>
/// Pushes persisted notifications to connected clients in real time.
/// Abstracted so business services remain independent of SignalR plumbing.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Delivers a notification to its recipient(s) over the notifications hub.
    /// </summary>
    /// <param name="notification">The persisted notification to deliver.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DispatchAsync(Notification notification, CancellationToken cancellationToken = default);
}

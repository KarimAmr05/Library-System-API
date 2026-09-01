using LibrarySystem.Business.DTOs.Notifications;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Interfaces;

/// <summary>
/// Notification inbox operations plus creation/dispatch helpers used by the
/// borrowing workflow and background jobs.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Lists notifications for the calling principal. Non-admin callers are always
    /// scoped to their own inbox regardless of supplied filters.
    /// </summary>
    /// <param name="query">Paging filters; may narrow an admin feed.</param>
    /// <param name="callerId">Authenticated user id.</param>
    /// <param name="callerRole">Authenticated role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged result of notifications or a validation failure.</returns>
    Task<Result<PagedResult<NotificationDto>>> GetNotificationsAsync(
        NotificationsQueryDto query,
        Guid callerId,
        UserRole callerRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks one of the caller's notifications as read.
    /// </summary>
    /// <param name="id">Notification identifier.</param>
    /// <param name="callerId">Authenticated user id.</param>
    /// <param name="callerRole">Authenticated role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated notification or a descriptive failure.</returns>
    Task<Result<NotificationDto>> MarkAsReadAsync(
        Guid id,
        Guid callerId,
        UserRole callerRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a notification and pushes it in real time through the dispatcher.
    /// </summary>
    /// <param name="notification">The notification to persist and deliver.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task CreateAndDispatchAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends due-date reminder notifications for a borrowing request, de-duplicated
    /// per recipient so reminders are never repeated unnecessarily.
    /// </summary>
    /// <param name="recipientUserId">Recipient of the reminder.</param>
    /// <param name="recipientRole">Role of the recipient.</param>
    /// <param name="relatedRequestId">Borrowing request the reminder relates to.</param>
    /// <param name="title">Reminder title.</param>
    /// <param name="message">Reminder message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SendDueReminderAsync(
        Guid recipientUserId,
        UserRole recipientRole,
        Guid relatedRequestId,
        string title,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identifiers of all active administrators.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The admin user ids.</returns>
    Task<IReadOnlyList<Guid>> GetActiveAdminIdsAsync(CancellationToken cancellationToken = default);
}

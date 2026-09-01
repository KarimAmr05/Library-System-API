using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;

namespace LibrarySystem.DataAccess.Interfaces;

/// <summary>
/// Repository contract with notification-specific read operations.
/// </summary>
public interface INotificationRepository : IGenericRepository<Notification>
{
    /// <summary>
    /// Returns one page of notifications scoped to a recipient.
    /// Admins may additionally receive role-wide feeds.
    /// </summary>
    /// <param name="page">1-based page index.</param>
    /// <param name="pageSize">Records per page.</param>
    /// <param name="recipientUserId">Recipient scope.</param>
    /// <param name="recipientRole">Optional role filter.</param>
    /// <param name="isRead">Optional read-state filter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The paged items together with the total matching count.</returns>
    Task<(IReadOnlyList<Notification> Items, long TotalItems)> GetPagedForRecipientAsync(
        int page,
        int pageSize,
        Guid recipientUserId,
        UserRole? recipientRole,
        bool? isRead,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a reminder notification has already been created for the
    /// given request and recipient, preventing duplicate reminders.
    /// </summary>
    /// <param name="recipientUserId">Recipient identifier.</param>
    /// <param name="relatedRequestId">Borrowing request the reminder relates to.</param>
    /// <param name="type">Notification type to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> when such a notification already exists.</returns>
    Task<bool> ExistsForRequestAsync(
        Guid recipientUserId,
        Guid relatedRequestId,
        NotificationType type,
        CancellationToken cancellationToken = default);
}

using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Interfaces;
using LibrarySystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.DataAccess.Repositories;

/// <summary>
/// Notification repository with recipient-scoped paging and reminder de-duplication support.
/// </summary>
/// <param name="context">The database context.</param>
public class NotificationRepository(DbContext context)
    : GenericRepository<Notification>(context), INotificationRepository
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<Notification> Items, long TotalItems)> GetPagedForRecipientAsync(
        int page,
        int pageSize,
        Guid recipientUserId,
        UserRole? recipientRole,
        bool? isRead,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(n => n.RecipientUserId == recipientUserId);

        if (recipientRole.HasValue)
        {
            query = query.Where(n => n.RecipientRole == recipientRole.Value);
        }

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        long totalItems = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalItems);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForRequestAsync(
        Guid recipientUserId,
        Guid relatedRequestId,
        NotificationType type,
        CancellationToken cancellationToken = default) =>
        await Query()
            .AnyAsync(n =>
                n.RecipientUserId == recipientUserId &&
                n.RelatedRequestId == relatedRequestId &&
                n.Type == type,
                cancellationToken)
            .ConfigureAwait(false);
}

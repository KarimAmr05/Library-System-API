using LibrarySystem.Business.DTOs.Notifications;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Business.Mappings;
using LibrarySystem.Business.Notifications;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Business.Services;

/// <summary>
/// Default notification service: persists notifications through the Unit of Work
/// and pushes them in real time via <see cref="INotificationDispatcher"/>.
/// </summary>
/// <param name="unitOfWork">Unit of work for persistence.</param>
/// <param name="dispatcher">Real-time notification dispatcher.</param>
/// <param name="logger">Structured logger.</param>
public sealed class NotificationService(
    IUnitOfWork unitOfWork,
    INotificationDispatcher dispatcher,
    ILogger<NotificationService> logger) : INotificationService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly INotificationDispatcher _dispatcher =
        dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private readonly ILogger<NotificationService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<Result<PagedResult<NotificationDto>>> GetNotificationsAsync(
        NotificationsQueryDto query,
        Guid callerId,
        UserRole callerRole,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = Validators.DtoValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<NotificationDto>>([.. validation.Errors]);
        }

        // Security: non-admins are hard-scoped to their own inbox; any supplied
        // recipientUserId filter is overridden.
        var effectiveUserId = callerId;
        UserRole? effectiveRole = query.RecipientRole;

        if (callerRole != UserRole.Admin)
        {
            effectiveUserId = callerId;
            effectiveRole = null;
        }
        else if (query.RecipientUserId.HasValue)
        {
            effectiveUserId = query.RecipientUserId.Value;
        }

        var (items, totalItems) = await _unitOfWork.Notifications.GetPagedForRecipientAsync(
            query.Page,
            query.PageSize,
            effectiveUserId,
            effectiveRole,
            query.IsRead,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(PagedResult<NotificationDto>.Create(
            items.Select(n => n.ToDto()).ToList(),
            query.Page,
            query.PageSize,
            totalItems));
    }

    /// <inheritdoc />
    public async Task<Result<NotificationDto>> MarkAsReadAsync(
        Guid id,
        Guid callerId,
        UserRole callerRole,
        CancellationToken cancellationToken = default)
    {
        var tracked = await _unitOfWork.Notifications.GetByIdTrackedAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (tracked is null)
        {
            return Result.Failure<NotificationDto>(Error.NotFound("Notification"));
        }

        // Ownership check: users may only mark their own notifications as read.
        if (callerRole != UserRole.Admin && tracked.RecipientUserId != callerId)
        {
            _logger.LogWarning(
                "User {CallerId} attempted to read foreign notification {NotificationId}.",
                callerId, id);
            return Result.Failure<NotificationDto>(
                Error.NotFound("Notification"));
        }

        if (!tracked.IsRead)
        {
            tracked.IsRead = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(tracked.ToDto());
    }

    /// <inheritdoc />
    public async Task CreateAndDispatchAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _dispatcher.DispatchAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SendDueReminderAsync(
        Guid recipientUserId,
        UserRole recipientRole,
        Guid relatedRequestId,
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        // De-duplication: never send the same reminder twice for one request/recipient.
        var alreadySent = await _unitOfWork.Notifications.ExistsForRequestAsync(
            recipientUserId, relatedRequestId, NotificationType.BorrowDueReminder, cancellationToken)
            .ConfigureAwait(false);

        if (alreadySent)
        {
            return;
        }

        await CreateAndDispatchAsync(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            RecipientRole = recipientRole,
            Type = NotificationType.BorrowDueReminder,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedRequestId = relatedRequestId
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetActiveAdminIdsAsync(CancellationToken cancellationToken = default) =>
        (await _unitOfWork.Users.Query()
            .Where(u => u.Role == UserRole.Admin && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false)).ToList();
}

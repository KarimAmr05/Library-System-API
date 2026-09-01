using LibrarySystem.Business.Interfaces;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Business.BackgroundJobs;

/// <summary>
/// Periodic job that:
/// 1. Sends due-date reminders to borrowers and admins when a borrowed book's
///    period is nearing expiration (de-duplicated per recipient/request).
/// 2. Marks overdue approved requests as Expired and restores the copy to
///    availability.
/// The run interval and reminder window come from <see cref="BackgroundJobSettings"/>.
/// </summary>
/// <param name="serviceScopeFactory">Scope factory resolving scoped services per run.</param>
/// <param name="settings">Configurable job settings.</param>
/// <param name="logger">Structured logger.</param>
public sealed class BorrowingExpirationJob(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<BackgroundJobSettings> settings,
    ILogger<BorrowingExpirationJob> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory =
        serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly BackgroundJobSettings _settings =
        settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<BorrowingExpirationJob> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "BorrowingExpirationJob started. Interval: {IntervalMinutes} min, reminder window: {ReminderDays} days.",
            _settings.ExpirationCheckIntervalMinutes, _settings.ReminderDaysBeforeDue);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_settings.ExpirationCheckIntervalMinutes));

        // Run once immediately, then on every tick.
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BorrowingExpirationJob iteration failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));

        _logger.LogInformation("BorrowingExpirationJob stopped.");
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await SendRemindersAsync(unitOfWork, notificationService, cancellationToken).ConfigureAwait(false);
        await ExpireOverdueRequestsAsync(unitOfWork, notificationService, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendRemindersAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var windowStart = now;
        var windowEnd = now.AddDays(_settings.ReminderDaysBeforeDue);

        var approachingDue = await unitOfWork.BorrowingRequests
            .GetApprovedWithDueDateBetweenAsync(windowStart, windowEnd, cancellationToken)
            .ConfigureAwait(false);

        foreach (var request in approachingDue)
        {
            var dueDate = request.ReviewedAt!.Value.AddDays(request.BorrowingPeriodDays);
            var title = "Borrowing period ending soon";
            var message = $"'{request.BookTitle}' is due on {dueDate:u}. Please return it soon.";

            await notificationService.SendDueReminderAsync(
                request.UserId, UserRole.User, request.Id, title, message, cancellationToken)
                .ConfigureAwait(false);

            foreach (var adminId in await notificationService.GetActiveAdminIdsAsync(cancellationToken)
                         .ConfigureAwait(false))
            {
                await notificationService.SendDueReminderAsync(
                    adminId, UserRole.Admin, request.Id,
                    $"Due soon: {request.BookTitle}",
                    $"User's copy of '{request.BookTitle}' is due on {dueDate:u}.",
                    cancellationToken).ConfigureAwait(false);
            }
        }

        if (approachingDue.Count > 0)
        {
            _logger.LogInformation("Sent reminders for {Count} approaching borrowings.", approachingDue.Count);
        }
    }

    private async Task ExpireOverdueRequestsAsync(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var overdue = await unitOfWork.BorrowingRequests
            .GetOverdueApprovedAsync(now, cancellationToken)
            .ConfigureAwait(false);

        foreach (var request in overdue)
        {
            try
            {
                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    var tracked = await unitOfWork.BorrowingRequests
                        .GetByIdWithBookTrackedAsync(request.Id, ct).ConfigureAwait(false);

                    if (tracked is null || tracked.Status != BorrowingRequestStatus.Approved)
                    {
                        return;
                    }

                    tracked.Status = BorrowingRequestStatus.Expired;
                    tracked.Book.AvailableCopies++;
                    tracked.Book.IsAvailable = true;
                    tracked.Book.UpdatedAt = DateTime.UtcNow;
                }, cancellationToken).ConfigureAwait(false);

                await NotifyExpiredAsync(notificationService, request, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Marked borrowing {RequestId} as expired.", request.Id);
            }
            catch (Exception ex)
            {
                // One failing record must not block the remaining expirations.
                _logger.LogError(ex, "Failed to expire borrowing request {RequestId}.", request.Id);
            }
        }
    }

    private static async Task NotifyExpiredAsync(
        INotificationService notificationService,
        BorrowingRequest request,
        CancellationToken cancellationToken)
    {
        await notificationService.CreateAndDispatchAsync(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = request.UserId,
            RecipientRole = UserRole.User,
            Type = NotificationType.BorrowDueReminder,
            Title = "Borrowing period ended",
            Message = $"'{request.BookTitle}' was marked returned/expired. Please contact the library.",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedRequestId = request.Id
        }, cancellationToken).ConfigureAwait(false);
    }
}

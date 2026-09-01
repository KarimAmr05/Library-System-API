using LibrarySystem.Business.DTOs.Requests;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Business.Mappings;
using LibrarySystem.Business.Messaging;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Constants;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Business.Services;

/// <summary>
/// Borrowing workflow: request submission with queue hand-off, listing with
/// ownership scoping, and admin approval/denial executed inside database
/// transactions so book availability can never drift out of sync under
/// concurrent requests.
/// </summary>
/// <param name="unitOfWork">Unit of work coordinating persistence.</param>
/// <param name="publisher">RabbitMQ publisher for asynchronous processing.</param>
/// <param name="notificationService">Notification creation/dispatch service.</param>
/// <param name="logger">Structured logger.</param>
public sealed class BorrowingService(
    IUnitOfWork unitOfWork,
    IBorrowRequestPublisher publisher,
    INotificationService notificationService,
    ILogger<BorrowingService> logger) : IBorrowingService
{
    private const int MinPeriodDays = 1;
    private const int MaxPeriodDays = 30;

    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly IBorrowRequestPublisher _publisher =
        publisher ?? throw new ArgumentNullException(nameof(publisher));
    private readonly INotificationService _notificationService =
        notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    private readonly ILogger<BorrowingService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<Result<BorrowingRequestDto>> CreateRequestAsync(
        BorrowRequestCreateDto request,
        Guid authenticatedUserId,
        UserRole authenticatedRole,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<BorrowingRequestDto>([.. validation.Errors]);
        }

        // Security: a regular user may never borrow on behalf of someone else.
        // Admins may submit on behalf of users (e.g., counter assistance).
        if (authenticatedRole != UserRole.Admin && request.UserId != authenticatedUserId)
        {
            return Result.Failure<BorrowingRequestDto>(
                new Error(ErrorCodes.Forbidden, "You may only create borrowing requests for yourself."));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure<BorrowingRequestDto>(Error.NotFound("User"));
        }

        if (!user.IsActive)
        {
            return Result.Failure<BorrowingRequestDto>(
                Error.BusinessRule("Inactive users cannot submit borrowing requests.",
                    nameof(request.UserId)));
        }

        var book = await _unitOfWork.Books.GetByIdAsync(request.BookId, cancellationToken).ConfigureAwait(false);
        if (book is null)
        {
            return Result.Failure<BorrowingRequestDto>(Error.NotFound("Book"));
        }

        if (!book.IsAvailable || book.AvailableCopies <= 0)
        {
            return Result.Failure<BorrowingRequestDto>(
                Error.Conflict($"Book '{book.Title}' has no available copies right now."));
        }

        // A single SaveChanges is atomic; an explicit transaction adds no value here.
        var borrowingRequest = new BorrowingRequest
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            BookTitle = book.Title,
            UserId = user.Id,
            Status = BorrowingRequestStatus.Pending,
            BorrowingPeriodDays = request.BorrowingPeriodDays,
            RequestedAt = DateTime.UtcNow
        };

        await _unitOfWork.BorrowingRequests.AddAsync(borrowingRequest, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _publisher.PublishAsync(new BorrowRequestMessage(borrowingRequest.Id), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // The pending record remains persisted; admins can still review it via
            // GET /api/requests. Queue unavailability must not fail the documented
            // 201 contract path.
            _logger.LogError(ex, "Failed to publish borrow request {RequestId} to RabbitMQ.", borrowingRequest.Id);
        }

        _logger.LogInformation(
            "Borrow request {RequestId} created by user {UserId} for book {BookId} ({PeriodDays} days).",
            borrowingRequest.Id, user.Id, book.Id, request.BorrowingPeriodDays);

        return Result.Success(borrowingRequest.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<BorrowingRequestDto>>> GetRequestsAsync(
        RequestsListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = Validators.DtoValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<BorrowingRequestDto>>([.. validation.Errors]);
        }

        var result = await _unitOfWork.BorrowingRequests.GetPagedAsync(
            query.Page, query.PageSize, query.Status, query.UserId, query.BookId,
            query.FromDate, query.ToDate, cancellationToken).ConfigureAwait(false);

        return Result.Success(result.ToDto(query.Page, query.PageSize, r => r.ToDto()));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<BorrowingRequestDto>>> GetMyRequestsAsync(
        RequestsListQueryDto query,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = Validators.DtoValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<BorrowingRequestDto>>([.. validation.Errors]);
        }

        var result = await _unitOfWork.BorrowingRequests.GetPagedAsync(
            query.Page, query.PageSize, query.Status, userId, query.BookId,
            query.FromDate, query.ToDate, cancellationToken).ConfigureAwait(false);

        return Result.Success(result.ToDto(query.Page, query.PageSize, r => r.ToDto()));
    }

    /// <inheritdoc />
    public async Task<Result<BorrowingRequestDto>> GetRequestByIdAsync(
        Guid id,
        Guid callerId,
        UserRole callerRole,
        CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.BorrowingRequests.GetByIdAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (request is null || (callerRole != UserRole.Admin && request.UserId != callerId))
        {
            // Not-owned requests are reported as not found to avoid leaking existence.
            return Result.Failure<BorrowingRequestDto>(Error.NotFound("Borrowing request"));
        }

        return Result.Success(request.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<BorrowingRequestDto>> ApproveAsync(
        Guid id,
        BorrowRequestApproveDto request,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<BorrowingRequestDto>([.. validation.Errors]);
        }

        if (request.ApprovedByAdminId != adminId)
        {
            return Result.Failure<BorrowingRequestDto>(
                new Error(ErrorCodes.Forbidden,
                    "approvedByAdminId must match the authenticated administrator."));
        }

        // Transaction + tracked reload: concurrent approvals see committed state
        // because the tracked read happens inside the transaction after prior
        // writers have committed.
        var (outcome, tracked) = await _unitOfWork
            .ExecuteInTransactionAsync<(TransitionOutcome Outcome, BorrowingRequest? Request)>(async ct =>
        {
            var loaded = await _unitOfWork.BorrowingRequests.GetByIdWithBookTrackedAsync(id, ct)
                .ConfigureAwait(false);

            if (loaded is null)
            {
                return (TransitionOutcome.NotFound, null);
            }

            if (loaded.Status != BorrowingRequestStatus.Pending)
            {
                return (TransitionOutcome.InvalidState, loaded);
            }

            if (!loaded.Book.IsAvailable || loaded.Book.AvailableCopies <= 0)
            {
                return (TransitionOutcome.NoCopiesAvailable, loaded);
            }

            ApplyApproval(loaded, adminId);
            return (TransitionOutcome.Applied, loaded);
        }, cancellationToken).ConfigureAwait(false);

        switch (outcome)
        {
            case TransitionOutcome.NotFound:
                return Result.Failure<BorrowingRequestDto>(Error.NotFound("Borrowing request"));

            case TransitionOutcome.InvalidState:
                _logger.LogWarning("Attempt to approve non-pending request {RequestId} ({Status}).",
                    id, tracked!.Status);
                return Result.Failure<BorrowingRequestDto>(
                    Error.Conflict($"Request is already {tracked.Status}; only pending requests can be approved."));

            case TransitionOutcome.NoCopiesAvailable:
                return Result.Failure<BorrowingRequestDto>(
                    Error.Conflict($"Book '{tracked!.BookTitle}' has no available copies; cannot approve."));

            default:
                break;
        }

        await NotifyDecisionAsync(tracked!, approved: true, reason: null, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Borrow request {RequestId} approved by admin {AdminId}.", id, adminId);
        return Result.Success(tracked!.ToDto());
    }

    /// <inheritdoc />
    public async Task<Result<BorrowingRequestDto>> DenyAsync(
        Guid id,
        BorrowRequestDenyDto request,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = Validators.DtoValidator.Validate(request);
        if (validation.IsFailure)
        {
            return Result.Failure<BorrowingRequestDto>([.. validation.Errors]);
        }

        if (request.DeniedByAdminId != adminId)
        {
            return Result.Failure<BorrowingRequestDto>(
                new Error(ErrorCodes.Forbidden,
                    "deniedByAdminId must match the authenticated administrator."));
        }

        var (outcome, tracked) = await _unitOfWork
            .ExecuteInTransactionAsync<(TransitionOutcome Outcome, BorrowingRequest? Request)>(async ct =>
        {
            var loaded = await _unitOfWork.BorrowingRequests.GetByIdWithBookTrackedAsync(id, ct)
                .ConfigureAwait(false);

            if (loaded is null)
            {
                return (TransitionOutcome.NotFound, null);
            }

            if (loaded.Status != BorrowingRequestStatus.Pending)
            {
                return (TransitionOutcome.InvalidState, loaded);
            }

            loaded.Status = BorrowingRequestStatus.Denied;
            loaded.ReviewedAt = DateTime.UtcNow;
            loaded.ReviewedBy = adminId;
            loaded.DenyReason = request.Reason.Trim();

            return (TransitionOutcome.Applied, loaded);
        }, cancellationToken).ConfigureAwait(false);

        switch (outcome)
        {
            case TransitionOutcome.NotFound:
                return Result.Failure<BorrowingRequestDto>(Error.NotFound("Borrowing request"));

            case TransitionOutcome.InvalidState:
                _logger.LogWarning("Attempt to deny non-pending request {RequestId} ({Status}).",
                    id, tracked!.Status);
                return Result.Failure<BorrowingRequestDto>(
                    Error.Conflict($"Request is already {tracked.Status}; only pending requests can be denied."));

            default:
                break;
        }

        await NotifyDecisionAsync(tracked!, approved: false, reason: tracked!.DenyReason, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Borrow request {RequestId} denied by admin {AdminId}.", id, adminId);
        return Result.Success(tracked!.ToDto());
    }

    /// <inheritdoc />
    public async Task ProcessQueuedRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await _unitOfWork.BorrowingRequests.Query()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            .ConfigureAwait(false);

        // Unknown or already-transitioned requests are treated as duplicates/outdated:
        // safe to skip so redelivered messages remain idempotent.
        if (request is null)
        {
            _logger.LogWarning("Queued borrow request {RequestId} no longer exists; skipping.", requestId);
            return;
        }

        if (request.Status != BorrowingRequestStatus.Pending)
        {
            _logger.LogInformation(
                "Queued borrow request {RequestId} already processed (status {Status}); skipping.",
                requestId, request.Status);
            return;
        }

        var admins = await _notificationService.GetActiveAdminIdsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var adminId in admins)
        {
            await _notificationService.CreateAndDispatchAsync(new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = adminId,
                RecipientRole = UserRole.Admin,
                Type = NotificationType.BorrowRequestCreated,
                Title = "New borrowing request",
                Message = $"'{request.BookTitle}' was requested for {request.BorrowingPeriodDays} days.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                RelatedRequestId = request.Id
            }, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "Queued borrow request {RequestId} processed; {AdminCount} admins notified.",
            requestId, admins.Count);
    }

    private void ApplyApproval(BorrowingRequest tracked, Guid adminId)
    {
        tracked.Status = BorrowingRequestStatus.Approved;
        tracked.ReviewedAt = DateTime.UtcNow;
        tracked.ReviewedBy = adminId;
        tracked.DenyReason = null;

        tracked.Book.AvailableCopies--;
        tracked.Book.IsAvailable = tracked.Book.AvailableCopies > 0;
        tracked.Book.UpdatedAt = DateTime.UtcNow;
    }

    private async Task NotifyDecisionAsync(
        BorrowingRequest request,
        bool approved,
        string? reason,
        CancellationToken cancellationToken)
    {
        await _notificationService.CreateAndDispatchAsync(new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = request.UserId,
            RecipientRole = UserRole.User,
            Type = approved ? NotificationType.RequestApproved : NotificationType.RequestDenied,
            Title = approved ? "Borrowing request approved" : "Borrowing request denied",
            Message = approved
                ? $"Your request for '{request.BookTitle}' has been approved."
                : $"Your request for '{request.BookTitle}' was denied. Reason: {reason}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            RelatedRequestId = request.Id
        }, cancellationToken).ConfigureAwait(false);
    }
}

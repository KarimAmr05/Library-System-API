using LibrarySystem.Business.DTOs.Requests;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Interfaces;

/// <summary>
/// Borrowing-request workflow: submission, listing, review decisions
/// and asynchronous queue-driven processing.
/// </summary>
public interface IBorrowingService
{
    /// <summary>
    /// Creates a pending borrowing request and publishes it to RabbitMQ
    /// for asynchronous processing.
    /// </summary>
    /// <param name="request">The request payload from the client.</param>
    /// <param name="authenticatedUserId">User id resolved from JWT claims.</param>
    /// <param name="authenticatedRole">Role resolved from JWT claims.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created request or a descriptive failure.</returns>
    Task<Result<BorrowingRequestDto>> CreateRequestAsync(
        BorrowRequestCreateDto request,
        Guid authenticatedUserId,
        UserRole authenticatedRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists borrowing requests for admin review with paging/filtering.
    /// </summary>
    /// <param name="query">Filter parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged result of requests or a validation failure.</returns>
    Task<Result<PagedResult<BorrowingRequestDto>>> GetRequestsAsync(
        RequestsListQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the calling user's own borrowing history.
    /// </summary>
    /// <param name="query">Filter parameters.</param>
    /// <param name="userId">The authenticated user's id.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged result of requests or a validation failure.</returns>
    Task<Result<PagedResult<BorrowingRequestDto>>> GetMyRequestsAsync(
        RequestsListQueryDto query,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single borrowing request. Regular users may only view their own requests.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="callerId">Authenticated user id.</param>
    /// <param name="callerRole">Authenticated role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The request or a descriptive failure.</returns>
    Task<Result<BorrowingRequestDto>> GetRequestByIdAsync(
        Guid id,
        Guid callerId,
        UserRole callerRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a pending request, decrementing book availability atomically.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="request">Approval payload.</param>
    /// <param name="adminId">Authenticated admin id from JWT claims.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The approved request or a descriptive failure.</returns>
    Task<Result<BorrowingRequestDto>> ApproveAsync(
        Guid id,
        BorrowRequestApproveDto request,
        Guid adminId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Denies a pending request with a mandatory reason.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="request">Denial payload.</param>
    /// <param name="adminId">Authenticated admin id from JWT claims.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The denied request or a descriptive failure.</returns>
    Task<Result<BorrowingRequestDto>> DenyAsync(
        Guid id,
        BorrowRequestDenyDto request,
        Guid adminId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue-driven processing of a submitted borrowing request: re-validates
    /// state and notifies administrators in real time. Invoked by the RabbitMQ consumer.
    /// </summary>
    /// <param name="requestId">Identifier of the persisted pending request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ProcessQueuedRequestAsync(Guid requestId, CancellationToken cancellationToken = default);
}

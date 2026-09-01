using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;

namespace LibrarySystem.DataAccess.Interfaces;

/// <summary>
/// Repository contract with borrowing-request-specific read operations.
/// </summary>
public interface IBorrowingRequestRepository : IGenericRepository<BorrowingRequest>
{
    /// <summary>
    /// Returns one page of borrowing requests filtered/sorted at the database level.
    /// </summary>
    /// <param name="page">1-based page index.</param>
    /// <param name="pageSize">Records per page.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="userId">Optional requesting-user filter.</param>
    /// <param name="bookId">Optional book filter.</param>
    /// <param name="fromDate">Inclusive lower bound on RequestedAt.</param>
    /// <param name="toDate">Inclusive upper bound on RequestedAt (end of day).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The paged items together with the total matching count.</returns>
    Task<(IReadOnlyList<BorrowingRequest> Items, long TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        BorrowingRequestStatus? status,
        Guid? userId,
        Guid? bookId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds approved borrowing requests whose due date falls inside the supplied window.
    /// Used by the expiration background job.
    /// </summary>
    /// <param name="fromUtc">Inclusive lower bound of the due-date window.</param>
    /// <param name="toUtc">Exclusive upper bound of the due-date window.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching requests including their book navigation.</returns>
    Task<IReadOnlyList<BorrowingRequest>> GetApprovedWithDueDateBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds approved borrowing requests whose due date has already passed.
    /// Used by the expiration background job.
    /// </summary>
    /// <param name="nowUtc">Current UTC timestamp.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The overdue requests including their book navigation.</returns>
    Task<IReadOnlyList<BorrowingRequest>> GetOverdueApprovedAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a tracked borrowing request including its book for state transitions
    /// such as approval, denial or expiration. Tracked intentionally so availability
    /// updates are persisted through the Unit of Work.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The tracked request or <c>null</c>.</returns>
    Task<BorrowingRequest?> GetByIdWithBookTrackedAsync(Guid id, CancellationToken cancellationToken = default);
}

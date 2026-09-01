using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Interfaces;
using LibrarySystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.DataAccess.Repositories;

/// <summary>
/// Borrowing-request repository with database-level paging and expiration-job queries.
/// </summary>
/// <param name="context">The database context.</param>
public class BorrowingRequestRepository(DbContext context)
    : GenericRepository<BorrowingRequest>(context), IBorrowingRequestRepository
{
    private readonly DbContext _context = context;

    /// <inheritdoc />
    public async Task<(IReadOnlyList<BorrowingRequest> Items, long TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        BorrowingRequestStatus? status,
        Guid? userId,
        Guid? bookId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = Query();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        if (bookId.HasValue)
        {
            query = query.Where(r => r.BookId == bookId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.RequestedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Inclusive upper bound: end of the supplied day.
            var inclusiveEnd = toDate.Value.Date.AddDays(1);
            query = query.Where(r => r.RequestedAt < inclusiveEnd);
        }

        long totalItems = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalItems);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BorrowingRequest>> GetApprovedWithDueDateBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default) =>
        await Query()
            .Where(r =>
                r.Status == BorrowingRequestStatus.Approved &&
                r.ReviewedAt != null &&
                r.ReviewedAt.Value.AddDays(r.BorrowingPeriodDays) >= fromUtc &&
                r.ReviewedAt.Value.AddDays(r.BorrowingPeriodDays) < toUtc)
            .Include(r => r.Book)
            .AsTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BorrowingRequest>> GetOverdueApprovedAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default) =>
        await Query()
            .Where(r =>
                r.Status == BorrowingRequestStatus.Approved &&
                r.ReviewedAt != null &&
                r.ReviewedAt.Value.AddDays(r.BorrowingPeriodDays) <= nowUtc)
            .Include(r => r.Book)
            .AsTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<BorrowingRequest?> GetByIdWithBookTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<BorrowingRequest>()
            .Include(r => r.Book)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
}

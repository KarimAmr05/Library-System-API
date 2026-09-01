using System.Linq.Expressions;
using LibrarySystem.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.DataAccess.Repositories;

/// <summary>
/// Generic EF Core repository. Read paths use <c>AsNoTracking</c>; tracked
/// materialization is isolated in <see cref="GetByIdTrackedAsync"/> so callers
/// cannot accidentally update entities loaded for display.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <param name="context">The database context.</param>
public class GenericRepository<TEntity>(DbContext context) : IGenericRepository<TEntity>
    where TEntity : class
{
    private readonly DbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public IQueryable<TEntity> Query() => _context.Set<TEntity>().AsNoTracking();

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<TEntity>().FindAsync([id], cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<TEntity>()
            .FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await Query().AnyAsync(predicate, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<long> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = Query();
        return predicate is null
            ? await query.LongCountAsync(cancellationToken).ConfigureAwait(false)
            : await query.LongCountAsync(predicate, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _context.Set<TEntity>().AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await _context.Set<TEntity>().AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Update(TEntity entity) => _context.Set<TEntity>().Update(entity);

    /// <inheritdoc />
    public void Remove(TEntity entity) => _context.Set<TEntity>().Remove(entity);

    /// <inheritdoc />
    public void RemoveRange(IEnumerable<TEntity> entities) => _context.Set<TEntity>().RemoveRange(entities);
}

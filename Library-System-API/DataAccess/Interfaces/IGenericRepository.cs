using System.Linq.Expressions;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.DataAccess.Interfaces;

/// <summary>
/// Generic repository abstraction over common persistence operations for an entity type.
/// Read operations return untracked data by default; tracked entities are only
/// materialized through methods explicitly intended for updates.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IGenericRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets a queryable over all entities for composed, database-level read queries.
    /// Callers must materialize with async operators; no data is fetched until enumerated.
    /// </summary>
    /// <returns>An <see cref="IQueryable{TEntity}"/> over the entity set.</returns>
    IQueryable<TEntity> Query();

    /// <summary>
    /// Gets an entity by its primary key without change tracking.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching entity or <c>null</c>.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tracked entity by its primary key, intended for subsequent updates.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The matching tracked entity or <c>null</c>.</returns>
    Task<TEntity?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity matches the predicate.
    /// </summary>
    /// <param name="predicate">Filter expression.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> when at least one entity matches.</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching an optional predicate at the database level.
    /// </summary>
    /// <param name="predicate">Optional filter expression.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of matching entities.</returns>
    Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the context. Persisted on <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entity">Entity to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities in a single batch. Persisted on <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entities">Entities to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an entity as modified. Persisted on <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entity">Entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Marks an entity for deletion. Persisted on <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entity">Entity to remove.</param>
    void Remove(TEntity entity);

    /// <summary>
    /// Marks several entities for deletion. Persisted on <c>SaveChangesAsync</c>.
    /// </summary>
    /// <param name="entities">Entities to remove.</param>
    void RemoveRange(IEnumerable<TEntity> entities);
}

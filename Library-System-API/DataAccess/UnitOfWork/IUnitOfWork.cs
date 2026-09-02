using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Interfaces;

namespace LibrarySystem.DataAccess.UnitOfWork;

/// <summary>
/// Coordinates repository access and transactional persistence.
/// Repositories stage changes; only the Unit of Work commits them, ensuring
/// multi-entity business operations stay atomic.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Gets the book repository.</summary>
    IBookRepository Books { get; }

    /// <summary>Gets the borrowing-request repository.</summary>
    IBorrowingRequestRepository BorrowingRequests { get; }

    /// <summary>Gets the notification repository.</summary>
    INotificationRepository Notifications { get; }

    /// <summary>Gets the generic user repository.</summary>
    IGenericRepository<User> Users { get; }

    /// <summary>Gets the generic password-reset-token repository.</summary>
    IGenericRepository<PasswordResetToken> PasswordResetTokens { get; }

    /// <summary>Gets the generic refresh-token repository.</summary>
    IGenericRepository<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Persists all staged changes in a single round trip.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied operation inside a database transaction so that
    /// multiple changes commit atomically or not at all.
    /// </summary>
    /// <param name="operation">The work to execute within the transaction.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied operation inside a database transaction and returns its result.
    /// All staged changes commit atomically on success.
    /// </summary>
    /// <typeparam name="T">Result type of the operation.</typeparam>
    /// <param name="operation">The work to execute within the transaction.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The operation's result once the transaction has committed.</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied operation inside a database transaction with an
    /// explicit isolation level (e.g. serializable checks that must observe a
    /// consistent database state against concurrent writers).
    /// </summary>
    /// <typeparam name="T">Result type of the operation.</typeparam>
    /// <param name="operation">The work to execute within the transaction.</param>
    /// <param name="isolationLevel">Transaction isolation level to use.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The operation's result once the transaction has committed.</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
}

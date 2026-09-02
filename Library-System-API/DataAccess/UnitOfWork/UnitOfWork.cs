using LibrarySystem.DataAccess.Context;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Interfaces;
using LibrarySystem.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibrarySystem.DataAccess.UnitOfWork;

/// <summary>
/// Default <see cref="IUnitOfWork"/> implementation backed by a scoped
/// <see cref="LibraryDBContext"/>. Lazily instantiates repositories and owns
/// transaction boundaries for consistency-critical operations.
/// </summary>
/// <param name="context">The scoped database context.</param>
public class UnitOfWork(LibraryDBContext context) : IUnitOfWork
{
    private readonly LibraryDBContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private IDbContextTransaction? _currentTransaction;

    private IBookRepository? _books;
    private IBorrowingRequestRepository? _borrowingRequests;
    private INotificationRepository? _notifications;
    private IGenericRepository<User>? _users;
    private IGenericRepository<PasswordResetToken>? _passwordResetTokens;
    private IGenericRepository<RefreshToken>? _refreshTokens;

    /// <inheritdoc />
    public IBookRepository Books => _books ??= new BookRepository(_context);

    /// <inheritdoc />
    public IBorrowingRequestRepository BorrowingRequests =>
        _borrowingRequests ??= new BorrowingRequestRepository(_context);

    /// <inheritdoc />
    public INotificationRepository Notifications =>
        _notifications ??= new NotificationRepository(_context);

    /// <inheritdoc />
    public IGenericRepository<User> Users => _users ??= new GenericRepository<User>(_context);

    /// <inheritdoc />
    public IGenericRepository<PasswordResetToken> PasswordResetTokens =>
        _passwordResetTokens ??= new GenericRepository<PasswordResetToken>(_context);

    /// <inheritdoc />
    public IGenericRepository<RefreshToken> RefreshTokens =>
        _refreshTokens ??= new GenericRepository<RefreshToken>(_context);

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) =>
        await ExecuteInTransactionAsync<object?>(async ct =>
        {
            await operation(ct).ConfigureAwait(false);
            return null;
        }, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Reuse an ambient transaction when already inside one; otherwise begin a new one.
        if (_currentTransaction is not null)
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _currentTransaction = transaction;
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            _currentTransaction = null;
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        System.Data.IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Reuse an ambient transaction when already inside one; otherwise begin a new one.
        if (_currentTransaction is not null)
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

        try
        {
            _currentTransaction = transaction;
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            _currentTransaction = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync().ConfigureAwait(false);
            _currentTransaction = null;
        }
    }
}

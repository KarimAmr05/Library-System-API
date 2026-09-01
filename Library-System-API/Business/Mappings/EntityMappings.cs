using LibrarySystem.Business.DTOs.Books;
using LibrarySystem.Business.DTOs.Notifications;
using LibrarySystem.Business.DTOs.Requests;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Mappings;

/// <summary>
/// Manual entity-to-DTO mapping extensions. Chosen over a mapping library to keep
/// the mapping surface explicit, dependency-free and easy to audit against the API contract.
/// </summary>
public static class EntityMappings
{
    /// <summary>Maps a book entity to its API DTO.</summary>
    /// <param name="book">The source entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static BookDto ToDto(this Book book) => new()
    {
        Id = book.Id,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Category = book.Category,
        IsAvailable = book.IsAvailable,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        CreatedAt = book.CreatedAt,
        UpdatedAt = book.UpdatedAt
    };

    /// <summary>Maps a borrowing request entity to its API DTO.</summary>
    /// <param name="request">The source entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static BorrowingRequestDto ToDto(this BorrowingRequest request) => new()
    {
        Id = request.Id,
        BookId = request.BookId,
        BookTitle = request.BookTitle,
        UserId = request.UserId,
        Status = request.Status.ToString(),
        BorrowingPeriodDays = request.BorrowingPeriodDays,
        RequestedAt = request.RequestedAt,
        ReviewedAt = request.ReviewedAt,
        ReviewedBy = request.ReviewedBy,
        DenyReason = request.DenyReason
    };

    /// <summary>Maps a notification entity to its API DTO.</summary>
    /// <param name="notification">The source entity.</param>
    /// <returns>The mapped DTO.</returns>
    public static NotificationDto ToDto(this Notification notification) => new()
    {
        Id = notification.Id,
        RecipientUserId = notification.RecipientUserId,
        RecipientRole = notification.RecipientRole.ToString(),
        Type = notification.Type.ToString(),
        Title = notification.Title,
        Message = notification.Message,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };

    /// <summary>Wraps a paged entity collection into a paged DTO envelope.</summary>
    /// <param name="source">Paged entities plus total count.</param>
    /// <param name="page">Page index used.</param>
    /// <param name="pageSize">Page size used.</param>
    /// <param name="map">Element mapper.</param>
    /// <typeparam name="TSource">Source entity type.</typeparam>
    /// <typeparam name="TTarget">Target DTO type.</typeparam>
    /// <returns>A paged result of DTOs.</returns>
    public static PagedResult<TTarget> ToDto<TSource, TTarget>(
        this (IReadOnlyList<TSource> Items, long TotalItems) source,
        int page,
        int pageSize,
        Func<TSource, TTarget> map) =>
        PagedResult<TTarget>.Create(
            source.Items.Select(map).ToList(),
            page,
            pageSize,
            source.TotalItems);
}

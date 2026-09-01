using System.ComponentModel.DataAnnotations;
using LibrarySystem.Shared.Enums;

namespace LibrarySystem.Business.DTOs.Requests;

/// <summary>
/// Query parameters for GET /api/requests and GET /api/requests/my.
/// </summary>
public class RequestsListQueryDto
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>Gets or sets the 1-based page index (minimum 1).</summary>
    [Range(1, int.MaxValue)]
    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }

    /// <summary>Gets or sets the page size. Defaults to 20 and is capped at 100.</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get => _pageSize; set => _pageSize = value is < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize); }

    /// <summary>Gets or sets the optional lifecycle status filter.</summary>
    public BorrowingRequestStatus? Status { get; init; }

    /// <summary>Gets or sets the optional requesting-user filter (admin usage only).</summary>
    public Guid? UserId { get; init; }

    /// <summary>Gets or sets the optional book filter.</summary>
    public Guid? BookId { get; init; }

    /// <summary>Gets or sets the inclusive lower bound on the submission date.</summary>
    public DateTime? FromDate { get; init; }

    /// <summary>Gets or sets the inclusive upper bound on the submission date.</summary>
    public DateTime? ToDate { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Books;

/// <summary>
/// Query parameters for GET /api/books.
/// </summary>
public class BookListQueryDto
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

    /// <summary>Gets or sets an optional free-text filter matched against title/author/category.</summary>
    public string? Search { get; init; }

    /// <summary>Gets or sets a value indicating whether to restrict results to available books.</summary>
    public bool? AvailableOnly { get; init; }

    /// <summary>Gets or sets the sort field: title, author or createdAt.</summary>
    public string? SortBy { get; init; }

    /// <summary>Gets or sets the sort direction: asc or desc.</summary>
    [RegularExpression("^(?i)(asc|desc)$", ErrorMessage = "sortOrder must be 'asc' or 'desc'.")]
    public string SortOrder { get; init; } = "asc";
}

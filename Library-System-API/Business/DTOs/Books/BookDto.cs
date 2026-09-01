namespace LibrarySystem.Business.DTOs.Books;

/// <summary>
/// Book resource exposed by the API. Mirrors the documented Book model.
/// </summary>
public class BookDto
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the optional ISBN.</summary>
    public string? Isbn { get; init; }

    /// <summary>Gets the book title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the primary author name.</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>Gets the optional category/genre.</summary>
    public string? Category { get; init; }

    /// <summary>Gets a value indicating whether the book is currently borrowable.</summary>
    public bool IsAvailable { get; init; }

    /// <summary>Gets the total number of copies owned.</summary>
    public int TotalCopies { get; init; }

    /// <summary>Gets the number of copies currently available.</summary>
    public int AvailableCopies { get; init; }

    /// <summary>Gets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Gets the last modification timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; init; }
}

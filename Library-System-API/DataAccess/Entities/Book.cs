namespace LibrarySystem.DataAccess.Entities;

/// <summary>
/// Represents a book held by the library together with its copy availability.
/// </summary>
public class Book
{
    /// <summary>Gets or sets the unique identifier of the book.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the optional ISBN of the book.</summary>
    public string? Isbn { get; set; }

    /// <summary>Gets or sets the title of the book.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the primary author name.</summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category/genre.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets a value indicating whether the book is currently borrowable.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Gets or sets the total number of physical copies owned by the library.</summary>
    public int TotalCopies { get; set; }

    /// <summary>Gets or sets the number of copies currently available for borrowing. Must never be negative.</summary>
    public int AvailableCopies { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the last modification.</summary>
    public DateTime UpdatedAt { get; set; }
}

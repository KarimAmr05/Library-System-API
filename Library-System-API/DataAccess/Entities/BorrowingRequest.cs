using LibrarySystem.Shared.Enums;

namespace LibrarySystem.DataAccess.Entities;

/// <summary>
/// Represents a borrowing request submitted by a user for a specific book.
/// </summary>
public class BorrowingRequest
{
    /// <summary>Gets or sets the unique identifier of the request.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the requested book.</summary>
    public Guid BookId { get; set; }

    /// <summary>Gets or sets the denormalized book title captured at request time for display purposes.</summary>
    public string BookTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the user who submitted the request.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the current status within the request lifecycle.</summary>
    public BorrowingRequestStatus Status { get; set; }

    /// <summary>Gets or sets the requested borrowing period in days (1–30).</summary>
    public int BorrowingPeriodDays { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the request was submitted.</summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which an admin approved or denied the request.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Gets or sets the identifier of the admin who reviewed the request.</summary>
    public Guid? ReviewedBy { get; set; }

    /// <summary>Gets or sets the reason supplied when the request is denied.</summary>
    public string? DenyReason { get; set; }

    /// <summary>Gets or sets the navigation to the requested book.</summary>
    public Book Book { get; set; } = null!;

    /// <summary>Gets or sets the navigation to the requesting user.</summary>
    public User User { get; set; } = null!;
}

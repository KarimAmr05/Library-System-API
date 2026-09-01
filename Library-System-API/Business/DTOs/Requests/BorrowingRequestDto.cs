namespace LibrarySystem.Business.DTOs.Requests;

/// <summary>
/// Borrowing request resource exposed by the API.
/// Mirrors the documented BorrowingRequest model.
/// </summary>
public class BorrowingRequestDto
{
    /// <summary>Gets the unique request identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the related book identifier.</summary>
    public Guid BookId { get; init; }

    /// <summary>Gets the denormalized book title captured at request time.</summary>
    public string BookTitle { get; init; } = string.Empty;

    /// <summary>Gets the identifier of the requesting user.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets the current status: Pending, Approved, Denied, Returned or Expired.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets the requested borrowing period in days.</summary>
    public int BorrowingPeriodDays { get; init; }

    /// <summary>Gets the submission timestamp (UTC).</summary>
    public DateTime RequestedAt { get; init; }

    /// <summary>Gets the approval/denial timestamp (UTC), when reviewed.</summary>
    public DateTime? ReviewedAt { get; init; }

    /// <summary>Gets the identifier of the admin who reviewed the request.</summary>
    public Guid? ReviewedBy { get; init; }

    /// <summary>Gets the denial reason, present when denied.</summary>
    public string? DenyReason { get; init; }
}

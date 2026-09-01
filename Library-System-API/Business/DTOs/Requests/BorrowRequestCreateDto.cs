using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Requests;

/// <summary>
/// Payload for POST /api/borrow — submitting a new borrowing request.
/// </summary>
public class BorrowRequestCreateDto
{
    /// <summary>Gets or sets the identifier of the book being requested. Must reference an existing book.</summary>
    [Required]
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the requesting user.
    /// Required per API contract; validated server-side against the JWT identity
    /// so a caller can never submit on behalf of another user.
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the requested borrowing period in days. Allowed range: 1–30.</summary>
    [Range(1, 30, ErrorMessage = "Must be between 1 and 30")]
    public int BorrowingPeriodDays { get; set; }
}

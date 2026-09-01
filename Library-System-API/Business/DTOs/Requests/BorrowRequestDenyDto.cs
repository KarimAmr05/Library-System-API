using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Requests;

/// <summary>
/// Payload for PUT /api/requests/{id}/deny.
/// </summary>
public class BorrowRequestDenyDto
{
    /// <summary>
    /// Gets or sets the denying admin's identifier.
    /// Required per API contract; validated server-side against the JWT identity.
    /// </summary>
    [Required]
    public Guid DeniedByAdminId { get; set; }

    /// <summary>Gets or sets the required denial reason communicated back to the requester.</summary>
    [Required, MinLength(3)]
    public string Reason { get; set; } = string.Empty;
}

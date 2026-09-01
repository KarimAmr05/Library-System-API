using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Business.DTOs.Requests;

/// <summary>
/// Payload for PUT /api/requests/{id}/approve.
/// </summary>
public class BorrowRequestApproveDto
{
    /// <summary>
    /// Gets or sets the approving admin's identifier.
    /// Required per API contract; validated server-side against the JWT identity.
    /// </summary>
    [Required]
    public Guid ApprovedByAdminId { get; set; }

    /// <summary>Gets or sets an optional approval note retained for audit purposes.</summary>
    public string? ApprovalNote { get; set; }
}

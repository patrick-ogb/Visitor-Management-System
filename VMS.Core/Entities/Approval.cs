using VMS.Core.Interfaces;

namespace VMS.Core.Entities;

public class Approval : IAuditableEntity
{
    public int ApprovalId { get; set; }
    public int? GuestInvitationId { get; set; }
    public int ApprovedBy { get; set; } // ApplicationUser who approved
    public bool IsApproved { get; set; }
    public string? Comments { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual GuestInvitation? GuestInvitation { get; set; }
    public virtual ApplicationUser Approver { get; set; } = null!;
}





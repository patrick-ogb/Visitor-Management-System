using VMS.Core.Enums;
using VMS.Core.Interfaces;

namespace VMS.Core.Entities;

public class GuestInvitation : IAuditableEntity
{
    public int GuestInvitationId { get; set; }
    public string InvitationNo { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public int? HostId { get; set; } // ApplicationUser ID (nullable, no longer required)
    public int? EnterpriseId { get; set; }
    public int? EnterpriseUserId { get; set; } // EnterpriseUser ID for walk-in requests
    public string HostName { get; set; } = string.Empty; // Store host name directly
    public string? HostEmail { get; set; } // Store host email for reference
    public HostType HostType { get; set; } = HostType.ApplicationUser; // Track source of host data
    public InvitationStatus Status { get; set; } = InvitationStatus.PendingApproval;
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
    public int NumberOfAdditionalGuests { get; set; } = 0;
    public string? InvitedOnBehalfOf { get; set; } // Name of person if proxy invitation
    public bool IsProxyInvitation { get; set; } = false;
    public string? PurposeOfInvitation { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual Guest Guest { get; set; } = null!;
    public virtual ApplicationUser? Host { get; set; } // Optional navigation property (backward compatibility)
    public virtual Enterprise? Enterprise { get; set; }
    public virtual ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}



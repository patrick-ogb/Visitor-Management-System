using VMS.Core.Interfaces;

namespace VMS.Core.Entities;

public class Enterprise : IAuditableEntity
{
    public int EnterpriseId { get; set; }
    public int? OnePortalId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<GuestInvitation> Invitations { get; set; } = new List<GuestInvitation>();
}





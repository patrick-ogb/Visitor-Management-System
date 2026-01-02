using VMS.Core.Interfaces;

namespace VMS.Core.Entities;

public class Guest : IAuditableEntity
{
    public int GuestId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<GuestInvitation> Invitations { get; set; } = new List<GuestInvitation>();
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}





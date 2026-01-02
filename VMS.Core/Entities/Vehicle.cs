using VMS.Core.Interfaces;

namespace VMS.Core.Entities;

public class Vehicle : IAuditableEntity
{
    public int VehicleId { get; set; }
    public int? GuestId { get; set; }
    public int? GuestInvitationId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Color { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navigation properties
    public virtual Guest? Guest { get; set; }
    public virtual GuestInvitation? GuestInvitation { get; set; }
}



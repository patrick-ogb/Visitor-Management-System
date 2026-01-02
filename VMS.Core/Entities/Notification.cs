using VMS.Core.Enums;

namespace VMS.Core.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int UserId { get; set; } // ApplicationUser ID of recipient
    public string Type { get; set; } = string.Empty; // Notification type
    public string Message { get; set; } = string.Empty; // Display message
    public bool IsRead { get; set; } = false;
    public int? ReferenceId { get; set; } // Business entity reference (GuestInvitationId)
    public NotificationDeliveryStatus DeliveryStatus { get; set; } = NotificationDeliveryStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}






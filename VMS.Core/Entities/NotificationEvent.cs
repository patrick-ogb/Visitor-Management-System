namespace VMS.Core.Entities;

public class NotificationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty; // InvitationCreated, InvitationApproved, etc.
    public int ReferenceId { get; set; } // GuestInvitationId
    public int TargetUserId { get; set; } // ApplicationUser ID of recipient
    public bool Processed { get; set; } = false;
    public string? AdditionalData { get; set; } // JSON for extra context
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}








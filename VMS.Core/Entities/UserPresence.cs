namespace VMS.Core.Entities;

public class UserPresence
{
    public int UserId { get; set; } // ApplicationUser ID (Primary Key)
    public bool IsOnline { get; set; } = false;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public string? ConnectionId { get; set; } // SignalR connection ID
    
    // Navigation property
    public virtual ApplicationUser? User { get; set; }
}





namespace VMS.Core.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public int? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
}





namespace VMS.Client.DTOs.Approvals;

public class ApproveRequestDto
{
    public string Type { get; set; } = string.Empty; // "Invitation" or "WalkIn"
    public int RequestId { get; set; }
    public bool IsApproved { get; set; }
    public string? Comments { get; set; }
}







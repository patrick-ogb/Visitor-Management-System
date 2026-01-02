namespace VMS.Client.DTOs.Dashboard;

public class VisitorsByDayDto
{
    public string Day { get; set; } = string.Empty; // "Mon", "Tue", etc.
    public int Count { get; set; }
}


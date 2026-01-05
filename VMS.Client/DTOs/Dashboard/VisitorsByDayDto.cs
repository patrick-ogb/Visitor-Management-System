namespace VMS.Client.DTOs.Dashboard;

public class VisitorsByDayDto
{
    public string Day { get; set; } = string.Empty; // "Mon", "Tue", etc. (for ByDayOfWeek mode)
    public DateTime? Date { get; set; } // Specific date (for ByDate mode)
    public string DateLabel { get; set; } = string.Empty; // Formatted date string (e.g., "Jan 15", "15/01")
    public int Count { get; set; }
}


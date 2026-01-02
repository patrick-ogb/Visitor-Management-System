namespace VMS.Client.DTOs.Dashboard;

public class VisitorsByHourDto
{
    public int Hour { get; set; } // 0-23
    public int Count { get; set; }
    public string HourLabel { get; set; } = string.Empty; // "8:00", "9:00", etc.
}



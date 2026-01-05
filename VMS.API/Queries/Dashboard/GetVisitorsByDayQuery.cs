using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Dashboard;

public enum VisitorsByDayViewMode
{
    ByDate = 0,
    ByDayOfWeek = 1
}

public class GetVisitorsByDayQuery : IRequest<BaseResponse<List<VisitorsByDayDto>>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? EnterpriseId { get; set; }
    public VisitorsByDayViewMode ViewMode { get; set; } = VisitorsByDayViewMode.ByDate; // Default to ByDate
}

public class VisitorsByDayDto
{
    public string Day { get; set; } = string.Empty; // "Mon", "Tue", etc. (for ByDayOfWeek mode)
    public DateTime? Date { get; set; } // Specific date (for ByDate mode)
    public string DateLabel { get; set; } = string.Empty; // Formatted date string (e.g., "Jan 15", "15/01")
    public int Count { get; set; }
}


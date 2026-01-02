using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Dashboard;

public class GetVisitorsByHourQuery : IRequest<BaseResponse<List<VisitorsByHourDto>>>
{
    public DateTime? Date { get; set; }
    public int? EnterpriseId { get; set; }
}

public class VisitorsByHourDto
{
    public int Hour { get; set; } // 0-23
    public int Count { get; set; }
    public string HourLabel { get; set; } = string.Empty; // "8:00", "9:00", etc.
}




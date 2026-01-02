using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Dashboard;

public class GetVisitorsByDayQuery : IRequest<BaseResponse<List<VisitorsByDayDto>>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? EnterpriseId { get; set; }
}

public class VisitorsByDayDto
{
    public string Day { get; set; } = string.Empty; // "Mon", "Tue", etc.
    public int Count { get; set; }
}


using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Dashboard;

public class GetDashboardMetricsQuery : IRequest<BaseResponse<DashboardMetricsDto>>
{
    public int? EnterpriseId { get; set; }
}

public class DashboardMetricsDto
{
    public int TotalVisitors { get; set; }
    public int TotalEnterprises { get; set; }
    public int WalkInRequests { get; set; }
}




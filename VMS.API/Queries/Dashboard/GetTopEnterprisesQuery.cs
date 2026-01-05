using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Dashboard;

public class GetTopEnterprisesQuery : IRequest<BaseResponse<List<TopEnterpriseDto>>>
{
    public int Count { get; set; } = 5; // Default to top 5
    public int? EnterpriseId { get; set; } // For filtering (non-SUPERADMIN users)
}

public class TopEnterpriseDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}



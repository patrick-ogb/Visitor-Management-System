using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Enterprises;

public class GetEnterprisesQuery : IRequest<BaseResponse<List<EnterpriseDto>>>
{
    public bool? IsActive { get; set; } = true;
}

public class EnterpriseDto
{
    public int EnterpriseId { get; set; }
    public int? OnePortalId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool IsActive { get; set; }
}


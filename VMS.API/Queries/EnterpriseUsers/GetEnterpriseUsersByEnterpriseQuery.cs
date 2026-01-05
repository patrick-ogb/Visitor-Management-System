using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.EnterpriseUsers;

public class GetEnterpriseUsersByEnterpriseQuery : IRequest<BaseResponse<List<Queries.Users.UserDto>>>
{
    public int? EnterpriseId { get; set; }
    public int? OnePortalEnterpriseId { get; set; }
}













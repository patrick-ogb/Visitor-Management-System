using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.EnterpriseUsers;

public class SyncEnterpriseUsersCommand : IRequest<BaseResponse<SyncEnterpriseUsersResponseDto>>
{
    public int OnePortalEnterpriseId { get; set; }
}

public class SyncEnterpriseUsersResponseDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Total { get; set; }
    public List<string> Errors { get; set; } = new();
}












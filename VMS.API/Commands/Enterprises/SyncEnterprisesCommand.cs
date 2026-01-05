using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Enterprises;

public class SyncEnterprisesCommand : IRequest<BaseResponse<SyncEnterprisesResponseDto>>
{
}

public class SyncEnterprisesResponseDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Total { get; set; }
    public List<string> Errors { get; set; } = new();
}













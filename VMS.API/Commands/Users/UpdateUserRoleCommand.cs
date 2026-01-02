using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Users;

public class UpdateUserRoleCommand : IRequest<BaseResponse<bool>>
{
    public int UserId { get; set; }
    public string NewRole { get; set; } = string.Empty;
    public int? EnterpriseId { get; set; }
}


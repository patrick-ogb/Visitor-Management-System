using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Gate;

public class RejectGuestCommand : IRequest<BaseResponse<bool>>
{
    public int GuestInvitationId { get; set; }
}















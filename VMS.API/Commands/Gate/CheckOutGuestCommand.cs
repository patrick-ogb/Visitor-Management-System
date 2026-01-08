using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Gate;

public class CheckOutGuestCommand : IRequest<BaseResponse<bool>>
{
    public int GuestInvitationId { get; set; }
}



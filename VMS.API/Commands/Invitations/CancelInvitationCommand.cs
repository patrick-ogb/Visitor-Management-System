using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Invitations;

public class CancelInvitationCommand : IRequest<BaseResponse<bool>>
{
    public int GuestInvitationId { get; set; }
    public int CreatedBy { get; set; } // For verification - set by controller
}










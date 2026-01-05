using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Gate;

public class RescheduleGuestInvitationCommand : IRequest<BaseResponse<bool>>
{
    public int GuestInvitationId { get; set; }
    public DateTime NewExpectedArrival { get; set; }
    public DateTime NewExpectedDeparture { get; set; }
}


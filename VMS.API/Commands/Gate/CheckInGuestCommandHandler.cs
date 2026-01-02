using MediatR;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;

namespace VMS.API.Commands.Gate;

public class CheckInGuestCommandHandler : IRequestHandler<CheckInGuestCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CheckInGuestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(CheckInGuestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(request.GuestInvitationId);
            if (invitation == null)
            {
                return BaseResponse<bool>.ErrorResponse("Invitation not found");
            }

            if (invitation.Status != InvitationStatus.Approved)
            {
                return BaseResponse<bool>.ErrorResponse("Only approved invitations can be checked in");
            }

            invitation.CheckedInAt = DateTime.UtcNow;
            invitation.Status = InvitationStatus.CheckedIn;
            _unitOfWork.GuestInvitations.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.SuccessResponse(true, "Guest checked in successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse(ex.Message);
        }
    }
}





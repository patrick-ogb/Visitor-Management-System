using MediatR;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;

namespace VMS.API.Commands.Gate;

public class RejectGuestCommandHandler : IRequestHandler<RejectGuestCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public RejectGuestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(RejectGuestCommand request, CancellationToken cancellationToken)
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
                return BaseResponse<bool>.ErrorResponse("Only approved invitations can be rejected");
            }

            invitation.Status = InvitationStatus.Rejected;
            _unitOfWork.GuestInvitations.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.SuccessResponse(true, "Guest entry rejected successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse(ex.Message);
        }
    }
}

















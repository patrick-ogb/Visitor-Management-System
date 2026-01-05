using MediatR;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;

namespace VMS.API.Commands.Gate;

public class CheckOutGuestCommandHandler : IRequestHandler<CheckOutGuestCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CheckOutGuestCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(CheckOutGuestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(request.GuestInvitationId);
            if (invitation == null)
            {
                return BaseResponse<bool>.ErrorResponse("Invitation not found");
            }

            // Verify guest is checked in
            if (!invitation.CheckedInAt.HasValue)
            {
                return BaseResponse<bool>.ErrorResponse("Guest is not checked in");
            }

            if (invitation.Status != InvitationStatus.CheckedIn)
            {
                return BaseResponse<bool>.ErrorResponse("Only checked-in guests can be checked out");
            }

            // Validate checkout time is within ExpectedArrival and ExpectedDeparture
            var currentTime = DateTime.UtcNow;
            if (currentTime < invitation.ExpectedArrival)
            {
                return BaseResponse<bool>.ErrorResponse("Cannot check out: Checkout time is before the expected arrival date");
            }

            if (currentTime > invitation.ExpectedDeparture)
            {
                return BaseResponse<bool>.ErrorResponse("Cannot check out: Checkout time is after the expected departure date");
            }

            // Process checkout - set CheckedInAt to null and update status
            invitation.CheckedInAt = null;
            invitation.Status = InvitationStatus.Completed;
            _unitOfWork.GuestInvitations.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.SuccessResponse(true, "Guest checked out successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse(ex.Message);
        }
    }
}


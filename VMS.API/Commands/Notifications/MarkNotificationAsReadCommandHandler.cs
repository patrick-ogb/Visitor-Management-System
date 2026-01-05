using MediatR;
using VMS.API.Common.Models;
using VMS.Infrastructure.Repositories;

namespace VMS.API.Commands.Notifications;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var notification = await _unitOfWork.Notifications
                .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == request.UserId);

            if (notification == null)
            {
                return BaseResponse<bool>.ErrorResponse("Notification not found");
            }

            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return BaseResponse<bool>.SuccessResponse(true, "Notification marked as read");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse(ex.Message);
        }
    }
}







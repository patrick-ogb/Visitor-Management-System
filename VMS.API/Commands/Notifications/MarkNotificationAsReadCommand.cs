using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Commands.Notifications;

public class MarkNotificationAsReadCommand : IRequest<BaseResponse<bool>>
{
    public Guid NotificationId { get; set; }
    public int UserId { get; set; } // For authorization - set by controller
}








using MediatR;
using VMS.API.Common.Models;
using VMS.Core.DTOs;

namespace VMS.API.Queries.Notifications;

public class GetUnreadNotificationsQuery : IRequest<BaseResponse<List<NotificationDto>>>
{
    public int UserId { get; set; }
    public int? Limit { get; set; } = 20;
}


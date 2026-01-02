using MediatR;
using VMS.API.Common.Models;
using VMS.Core.DTOs;

namespace VMS.API.Queries.Notifications;

public class GetNotificationHistoryQuery : IRequest<BaseResponse<List<NotificationDto>>>
{
    public int UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}


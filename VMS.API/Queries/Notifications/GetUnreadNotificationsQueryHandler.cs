using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.DTOs;
using VMS.Infrastructure.Repositories;

namespace VMS.API.Queries.Notifications;

public class GetUnreadNotificationsQueryHandler : IRequestHandler<GetUnreadNotificationsQuery, BaseResponse<List<NotificationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUnreadNotificationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<NotificationDto>>> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _unitOfWork.Notifications
                .FindAsync(n => n.UserId == request.UserId && !n.IsRead);

            var notificationList = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(request.Limit ?? 20)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,
                    IsRead = n.IsRead,
                    ReferenceId = n.ReferenceId,
                    CreatedAt = n.CreatedAt
                })
                .ToList();

            return BaseResponse<List<NotificationDto>>.SuccessResponse(notificationList);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<NotificationDto>>.ErrorResponse(ex.Message);
        }
    }
}


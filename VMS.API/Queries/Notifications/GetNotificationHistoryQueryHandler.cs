using MediatR;
using VMS.API.Common.Models;
using VMS.Core.DTOs;
using VMS.Infrastructure.Repositories;

namespace VMS.API.Queries.Notifications;

public class GetNotificationHistoryQueryHandler : IRequestHandler<GetNotificationHistoryQuery, BaseResponse<List<NotificationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNotificationHistoryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<NotificationDto>>> Handle(GetNotificationHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var notifications = await _unitOfWork.Notifications
                .FindAsync(n => n.UserId == request.UserId);

            var notificationList = notifications
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
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


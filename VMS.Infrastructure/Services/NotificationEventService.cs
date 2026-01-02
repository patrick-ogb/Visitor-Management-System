using VMS.Core.Entities;
using VMS.Infrastructure.Repositories;

namespace VMS.Infrastructure.Services;

public class NotificationEventService : INotificationEventService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationEventService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NotificationEvent> CreateNotificationEventAsync(
        string eventType,
        int referenceId,
        int targetUserId,
        string? additionalData = null,
        CancellationToken cancellationToken = default)
    {
        var notificationEvent = new NotificationEvent
        {
            EventType = eventType,
            ReferenceId = referenceId,
            TargetUserId = targetUserId,
            AdditionalData = additionalData,
            Processed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.NotificationEvents.AddAsync(notificationEvent);
        // Note: SaveChangesAsync should be called by the caller within the same transaction
        return notificationEvent;
    }
}


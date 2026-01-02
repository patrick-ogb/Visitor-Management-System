using VMS.Core.Entities;

namespace VMS.Infrastructure.Services;

public interface INotificationEventService
{
    Task<NotificationEvent> CreateNotificationEventAsync(
        string eventType,
        int referenceId,
        int targetUserId,
        string? additionalData = null,
        CancellationToken cancellationToken = default);
}


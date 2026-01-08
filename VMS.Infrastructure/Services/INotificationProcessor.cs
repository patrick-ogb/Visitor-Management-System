using VMS.Core.Entities;

namespace VMS.Infrastructure.Services;

public interface INotificationProcessor
{
    Task ProcessNotificationEventAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default);
}








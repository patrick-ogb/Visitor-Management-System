namespace VMS.Infrastructure.Services;

public interface IPresenceService
{
    void MarkUserOnline(int userId, string connectionId);
    void MarkUserOffline(int userId, string connectionId);
    bool IsUserOnline(int userId);
    Task SyncToDatabaseAsync(CancellationToken cancellationToken = default);
    Task CleanupStaleConnectionsAsync(CancellationToken cancellationToken = default);
}








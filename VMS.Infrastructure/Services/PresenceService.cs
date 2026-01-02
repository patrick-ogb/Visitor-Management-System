using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VMS.Core.Entities;
using VMS.Infrastructure.Repositories;

namespace VMS.Infrastructure.Services;

public class PresenceService : IPresenceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PresenceService> _logger;
    private readonly ConcurrentDictionary<int, UserPresenceInfo> _onlineUsers = new();
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private readonly int _heartbeatTimeoutSeconds;

    public PresenceService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<PresenceService> logger)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _heartbeatTimeoutSeconds = _configuration.GetValue<int>("NotificationSettings:HeartbeatTimeoutSeconds", 60);
    }

    public void MarkUserOnline(int userId, string connectionId)
    {
        _onlineUsers.AddOrUpdate(
            userId,
            new UserPresenceInfo
            {
                UserId = userId,
                IsOnline = true,
                LastHeartbeat = DateTime.UtcNow,
                ConnectionIds = new HashSet<string> { connectionId }
            },
            (key, existing) =>
            {
                existing.IsOnline = true;
                existing.LastHeartbeat = DateTime.UtcNow;
                if (!existing.ConnectionIds.Contains(connectionId))
                {
                    existing.ConnectionIds.Add(connectionId);
                }
                return existing;
            });
    }

    public void MarkUserOffline(int userId, string connectionId)
    {
        if (_onlineUsers.TryGetValue(userId, out var presence))
        {
            presence.ConnectionIds.Remove(connectionId);
            if (presence.ConnectionIds.Count == 0)
            {
                presence.IsOnline = false;
            }
        }
    }

    public bool IsUserOnline(int userId)
    {
        if (_onlineUsers.TryGetValue(userId, out var presence))
        {
            // Check if heartbeat is still valid
            var timeSinceHeartbeat = DateTime.UtcNow - presence.LastHeartbeat;
            if (timeSinceHeartbeat.TotalSeconds > _heartbeatTimeoutSeconds)
            {
                presence.IsOnline = false;
                return false;
            }
            return presence.IsOnline;
        }
        return false;
    }

    public async Task SyncToDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var kvp in _onlineUsers)
            {
                var userId = kvp.Key;
                var presenceInfo = kvp.Value;

                var dbPresence = await _unitOfWork.UserPresences.GetByIdAsync(userId);
                if (dbPresence == null)
                {
                    dbPresence = new UserPresence
                    {
                        UserId = userId,
                        IsOnline = presenceInfo.IsOnline,
                        LastHeartbeat = presenceInfo.LastHeartbeat,
                        ConnectionId = string.Join(",", presenceInfo.ConnectionIds)
                    };
                    await _unitOfWork.UserPresences.AddAsync(dbPresence);
                }
                else
                {
                    dbPresence.IsOnline = presenceInfo.IsOnline;
                    dbPresence.LastHeartbeat = presenceInfo.LastHeartbeat;
                    dbPresence.ConnectionId = string.Join(",", presenceInfo.ConnectionIds);
                    _unitOfWork.UserPresences.Update(dbPresence);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing presence to database");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task CleanupStaleConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var staleUsers = _onlineUsers
            .Where(kvp => kvp.Value.IsOnline && (now - kvp.Value.LastHeartbeat).TotalSeconds > _heartbeatTimeoutSeconds)
            .ToList();

        foreach (var kvp in staleUsers)
        {
            kvp.Value.IsOnline = false;
            _logger.LogInformation("Marked user {UserId} as offline due to stale heartbeat", kvp.Key);
        }
    }

    private class UserPresenceInfo
    {
        public int UserId { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public HashSet<string> ConnectionIds { get; set; } = new();
    }
}


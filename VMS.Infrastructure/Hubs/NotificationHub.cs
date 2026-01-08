using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VMS.Infrastructure.Services;

namespace VMS.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IPresenceService _presenceService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IPresenceService presenceService, ILogger<NotificationHub> logger)
    {
        _presenceService = presenceService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            Context.Abort();
            return;
        }

        // Add user to group for targeted notifications
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Mark user as online
        _presenceService.MarkUserOnline(userId, Context.ConnectionId);

        _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            // Remove from group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");

            // Mark user as offline
            _presenceService.MarkUserOffline(userId, Context.ConnectionId);

            _logger.LogInformation("User {UserId} disconnected. Connection: {ConnectionId}", userId, Context.ConnectionId);
        }

        if (exception != null)
        {
            _logger.LogError(exception, "User disconnected with error");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendHeartbeat()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            // Update heartbeat in presence service
            _presenceService.MarkUserOnline(userId, Context.ConnectionId);
        }
    }
}








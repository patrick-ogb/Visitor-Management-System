using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VMS.Core.DTOs;
using VMS.Core.Entities;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Hubs;
using VMS.Infrastructure.Repositories;

namespace VMS.Infrastructure.Services;

public class NotificationProcessor : INotificationProcessor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPresenceService _presenceService;
    private readonly IEmailService _emailService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationProcessor> _logger;
    private readonly VmsDbContext _context;

    private readonly int _aggregationWindowMinutes;

    public NotificationProcessor(
        IUnitOfWork unitOfWork,
        IPresenceService presenceService,
        IEmailService emailService,
        IHubContext<NotificationHub> hubContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger<NotificationProcessor> logger,
        VmsDbContext context)
    {
        _unitOfWork = unitOfWork;
        _presenceService = presenceService;
        _emailService = emailService;
        _hubContext = hubContext;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _aggregationWindowMinutes = _configuration.GetValue<int>("NotificationSettings:AggregationWindowMinutes", 5);
    }

    public async Task ProcessNotificationEventAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user is online
            var isOnline = _presenceService.IsUserOnline(notificationEvent.TargetUserId);

            // Determine notification type and message based on event type
            var (notificationType, message) = await GetNotificationTypeAndMessageAsync(notificationEvent, cancellationToken);

            // Check for aggregation
            var aggregatedNotification = await AggregateNotificationsAsync(
                notificationEvent.TargetUserId,
                notificationType,
                cancellationToken);

            Notification notification;
            if (aggregatedNotification != null)
            {
                // Update existing notification
                aggregatedNotification.Message = message;
                aggregatedNotification.CreatedAt = DateTime.UtcNow; // Update timestamp
                _unitOfWork.Notifications.Update(aggregatedNotification);
                notification = aggregatedNotification;
            }
            else
            {
                // Create new notification
                notification = new Notification
                {
                    UserId = notificationEvent.TargetUserId,
                    Type = notificationType,
                    Message = message,
                    IsRead = false,
                    ReferenceId = notificationEvent.ReferenceId,
                    DeliveryStatus = isOnline ? NotificationDeliveryStatus.Delivered : NotificationDeliveryStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Notifications.AddAsync(notification);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Deliver notification if user is online
            if (isOnline)
            {
                await DeliverNotificationViaSignalRAsync(notificationEvent.TargetUserId, notification, cancellationToken);
                
                // Update delivery status
                notification.DeliveryStatus = NotificationDeliveryStatus.Delivered;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Send email notification (always, async)
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendEmailNotificationAsync(notificationEvent, notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email notification for event {EventId}", notificationEvent.Id);
                }
            }, cancellationToken);

            // Mark event as processed
            notificationEvent.Processed = true;
            _unitOfWork.NotificationEvents.Update(notificationEvent);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification event {EventId}", notificationEvent.Id);
            throw;
        }
    }

    private async Task<Notification?> AggregateNotificationsAsync(
        int userId,
        string notificationType,
        CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-_aggregationWindowMinutes);

        var existingNotification = await _context.Notifications
            .Where(n => n.UserId == userId &&
                       n.Type == notificationType &&
                       n.CreatedAt >= windowStart &&
                       !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return existingNotification;
    }

    private async Task DeliverNotificationViaSignalRAsync(
        int userId,
        Notification notification,
        CancellationToken cancellationToken)
    {
        try
        {
            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Message = notification.Message,
                IsRead = notification.IsRead,
                ReferenceId = notification.ReferenceId,
                CreatedAt = notification.CreatedAt
            };

            await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notificationDto, cancellationToken);
            _logger.LogInformation("Notification {NotificationId} delivered to user {UserId} via SignalR", notification.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering notification {NotificationId} via SignalR", notification.Id);
            notification.DeliveryStatus = NotificationDeliveryStatus.Failed;
            throw;
        }
    }

    private async Task SendEmailNotificationAsync(
        NotificationEvent notificationEvent,
        Notification notification,
        CancellationToken cancellationToken)
    {
        // Get user email from ApplicationUser
        var user = await _userManager.FindByIdAsync(notificationEvent.TargetUserId.ToString());
        if (user == null || string.IsNullOrEmpty(user.Email))
        {
            _logger.LogWarning("Cannot send email notification: User {UserId} not found or has no email", notificationEvent.TargetUserId);
            return;
        }

        // Determine email method based on notification type
        switch (notification.Type)
        {
            case "InvitationCancelled":
                // Get invitation details for cancellation email
                var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(notificationEvent.ReferenceId);
                if (invitation?.Guest != null)
                {
                    await _emailService.SendInvitationCancellationNotificationAsync(
                        user.Email,
                        invitation.Guest.Name,
                        invitation.InvitationNo);
                }
                break;
            // Other email types are handled synchronously in command handlers
            // This is for non-critical notifications
            default:
                _logger.LogDebug("Email notification for type {Type} handled elsewhere", notification.Type);
                break;
        }
    }

    private async Task<(string Type, string Message)> GetNotificationTypeAndMessageAsync(NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        // Try to get InvitationNo from AdditionalData first
        string invitationNo = $"#{notificationEvent.ReferenceId}"; // Fallback to ReferenceId
        
        try
        {
            // Check AdditionalData first (most reliable, no database query needed)
            if (!string.IsNullOrEmpty(notificationEvent.AdditionalData))
            {
                try
                {
                    var additionalData = JsonSerializer.Deserialize<JsonElement>(notificationEvent.AdditionalData);
                    if (additionalData.TryGetProperty("InvitationNo", out var invitationNoElement))
                    {
                        invitationNo = invitationNoElement.GetString() ?? invitationNo;
                        _logger.LogDebug("Found InvitationNo {InvitationNo} in AdditionalData for ReferenceId {ReferenceId}", invitationNo, notificationEvent.ReferenceId);
                        // Successfully got InvitationNo from AdditionalData, skip database lookup
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse AdditionalData JSON for ReferenceId {ReferenceId}, falling back to database lookup", notificationEvent.ReferenceId);
                }
            }
            
            // Fall back to database lookup only if AdditionalData didn't contain InvitationNo
            if (invitationNo == $"#{notificationEvent.ReferenceId}")
            {
                // Use DbContext directly with explicit query to ensure we get the InvitationNo
                // Try multiple times with a small delay in case the invitation hasn't been committed yet
                var maxRetries = 3;
                var retryDelay = TimeSpan.FromMilliseconds(100);
                
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    var invitation = await _context.GuestInvitations
                        .AsNoTracking() // Use AsNoTracking to avoid any caching issues
                        .Where(i => i.GuestInvitationId == notificationEvent.ReferenceId)
                        .Select(i => new { i.InvitationNo })
                        .FirstOrDefaultAsync(cancellationToken);
                    
                    if (invitation != null && !string.IsNullOrEmpty(invitation.InvitationNo))
                    {
                        invitationNo = invitation.InvitationNo;
                        _logger.LogInformation("Found InvitationNo {InvitationNo} for ReferenceId {ReferenceId} from database (attempt {Attempt})", invitationNo, notificationEvent.ReferenceId, attempt + 1);
                        break;
                    }
                    
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogDebug("Invitation {ReferenceId} not found on attempt {Attempt}, retrying...", notificationEvent.ReferenceId, attempt + 1);
                        await Task.Delay(retryDelay, cancellationToken);
                    }
                }
                
                if (invitationNo == $"#{notificationEvent.ReferenceId}")
                {
                    _logger.LogWarning("Invitation {ReferenceId} not found or has no InvitationNo after {MaxRetries} attempts, using ReferenceId as fallback", notificationEvent.ReferenceId, maxRetries);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invitation {ReferenceId} for notification message, using ReferenceId as fallback", notificationEvent.ReferenceId);
        }

        return notificationEvent.EventType switch
        {
            "InvitationCreated" => ("ApprovalRequest", $"New invitation request requires your approval (Invitation {invitationNo})"),
            "WalkInCreated" => ("ApprovalRequest", $"New walk-in request requires your approval (Invitation {invitationNo})"),
            "InvitationApproved" => ("InvitationApproved", $"Your invitation has been approved (Invitation {invitationNo})"),
            "InvitationRejected" => ("InvitationRejected", $"Your invitation has been rejected (Invitation {invitationNo})"),
            "InvitationCancelled" => ("InvitationCancelled", $"Invitation has been cancelled (Invitation {invitationNo})"),
            "InvitationCheckedIn" => ("InvitationCheckedIn", $"Guest has checked in (Invitation {invitationNo})"),
            _ => ("General", $"New notification (Reference {invitationNo})")
        };
    }
}


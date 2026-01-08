using MediatR;
using Microsoft.AspNetCore.Http;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;
using VMS.Infrastructure.Services;

namespace VMS.API.Commands.Gate;

public class RescheduleGuestInvitationCommandHandler : IRequestHandler<RescheduleGuestInvitationCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationEventService _notificationEventService;
    private readonly NotificationEventQueue _eventQueue;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RescheduleGuestInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationEventService notificationEventService,
        NotificationEventQueue eventQueue,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _notificationEventService = notificationEventService;
        _eventQueue = eventQueue;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BaseResponse<bool>> Handle(RescheduleGuestInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(request.GuestInvitationId);
            if (invitation == null)
            {
                return BaseResponse<bool>.ErrorResponse("Invitation not found");
            }

            // Validate new dates
            if (request.NewExpectedDeparture <= request.NewExpectedArrival)
            {
                return BaseResponse<bool>.ErrorResponse("Departure date must be after arrival date");
            }

            // Get current user ID (GateAdmin who is rescheduling)
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            int? rescheduledById = null;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                rescheduledById = userId;
            }

            // Update invitation dates
            invitation.ExpectedArrival = request.NewExpectedArrival;
            invitation.ExpectedDeparture = request.NewExpectedDeparture;
            
            // Set status back to PendingApproval to require host re-approval
            invitation.Status = InvitationStatus.PendingApproval;
            
            // Set reschedule tracking fields
            invitation.IsRescheduled = true;
            invitation.RescheduledById = rescheduledById;
            invitation.RescheduledAt = DateTime.UtcNow;
            
            // Update timestamp
            invitation.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.GuestInvitations.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send notification to host about reschedule request
            try
            {
                if (invitation.HostId.HasValue)
                {
                    // Get guest name - try navigation property first, otherwise use a generic name
                    // Note: GetByIdAsync may not load navigation properties, so Guest might be null
                    var guestName = invitation.Guest?.Name ?? "Guest";
                    var additionalData = System.Text.Json.JsonSerializer.Serialize(new 
                    { 
                        InvitationNo = invitation.InvitationNo,
                        Message = $"Your invitation for {guestName} has been rescheduled by Gate Admin. Please review the new dates and approve."
                    });

                    var notificationEvent = await _notificationEventService.CreateNotificationEventAsync(
                        "InvitationRescheduled", // Event type for reschedule
                        invitation.GuestInvitationId,
                        invitation.HostId.Value,
                        additionalData: additionalData,
                        cancellationToken: cancellationToken);

                    // Save notification event (it was added to unit of work by CreateNotificationEventAsync)
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Enqueue for processing
                    await _eventQueue.EnqueueAsync(notificationEvent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the reschedule if notification fails
                // Notification failure should not block the reschedule operation
            }

            return BaseResponse<bool>.SuccessResponse(true, "Invitation rescheduled successfully. Host approval is required.");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse($"Error rescheduling invitation: {ex.Message}");
        }
    }
}


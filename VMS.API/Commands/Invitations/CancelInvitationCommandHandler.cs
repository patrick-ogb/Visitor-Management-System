using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using VMS.API.Common.Models;
using VMS.Core.Entities;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;
using VMS.Infrastructure.Services;

namespace VMS.API.Commands.Invitations;

public class CancelInvitationCommandHandler : IRequestHandler<CancelInvitationCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationEventService _notificationEventService;
    private readonly NotificationEventQueue _eventQueue;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CancelInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        INotificationEventService notificationEventService,
        NotificationEventQueue eventQueue,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _notificationEventService = notificationEventService;
        _eventQueue = eventQueue;
        _emailService = emailService;
        _userManager = userManager;
    }

    public async Task<BaseResponse<bool>> Handle(CancelInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(request.GuestInvitationId);
            if (invitation == null)
            {
                return BaseResponse<bool>.ErrorResponse("Invitation not found");
            }

            // Verify current user is the creator
            if (invitation.CreatedBy != request.CreatedBy)
            {
                return BaseResponse<bool>.ErrorResponse("Only the user who created the invitation can cancel it");
            }

            // Verify invitation has not been checked in
            if (invitation.CheckedInAt.HasValue)
            {
                return BaseResponse<bool>.ErrorResponse("Cannot cancel an invitation that has already been checked in");
            }

            // Update invitation status to Cancelled
            invitation.Status = InvitationStatus.Cancelled;
            invitation.UpdatedAt = DateTime.UtcNow;
            invitation.UpdatedBy = request.CreatedBy;
            _unitOfWork.GuestInvitations.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send email notification to guest
            if (invitation.Guest != null && !string.IsNullOrEmpty(invitation.Guest.Email))
            {
                await _emailService.SendInvitationCancellationNotificationAsync(
                    invitation.Guest.Email,
                    invitation.Guest.Name,
                    invitation.InvitationNo);
            }

            // Create notification event for host/creator if they have an account
            var eventsToEnqueue = new List<Core.Entities.NotificationEvent>();
            
            if (invitation.HostId.HasValue)
            {
                var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                var hostEvent = await _notificationEventService.CreateNotificationEventAsync(
                    "InvitationCancelled",
                    invitation.GuestInvitationId,
                    invitation.HostId.Value,
                    additionalData: additionalData,
                    cancellationToken: cancellationToken);
                eventsToEnqueue.Add(hostEvent);
            }
            
            if (invitation.CreatedBy.HasValue && invitation.CreatedBy != invitation.HostId)
            {
                var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                var creatorEvent = await _notificationEventService.CreateNotificationEventAsync(
                    "InvitationCancelled",
                    invitation.GuestInvitationId,
                    invitation.CreatedBy.Value,
                    additionalData: additionalData,
                    cancellationToken: cancellationToken);
                eventsToEnqueue.Add(creatorEvent);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Enqueue events for immediate processing
            foreach (var evt in eventsToEnqueue)
            {
                await _eventQueue.EnqueueAsync(evt, cancellationToken);
            }

            return BaseResponse<bool>.SuccessResponse(true, "Invitation cancelled successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse(ex.Message);
        }
    }
}




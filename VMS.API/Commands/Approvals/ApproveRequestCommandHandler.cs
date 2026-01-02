using MediatR;
using System.Text.Json;
using VMS.API.Common.Models;
using VMS.Core.Entities;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;
using VMS.Infrastructure.Services;

namespace VMS.API.Commands.Approvals;

public class ApproveRequestCommandHandler : IRequestHandler<ApproveRequestCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly INotificationEventService _notificationEventService;
    private readonly NotificationEventQueue _eventQueue;

    public ApproveRequestCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        INotificationEventService notificationEventService,
        NotificationEventQueue eventQueue)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _notificationEventService = notificationEventService;
        _eventQueue = eventQueue;
    }

    public async Task<BaseResponse<bool>> Handle(ApproveRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Walk-ins are now GuestInvitations, so handle all as invitations
            if (request.Type == "Invitation" || request.Type == "WalkIn")
            {
                var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(request.RequestId);
                if (invitation == null)
                {
                    return BaseResponse<bool>.ErrorResponse("Invitation not found");
                }

                invitation.Status = request.IsApproved ? InvitationStatus.Approved : InvitationStatus.Rejected;
                _unitOfWork.GuestInvitations.Update(invitation);

                // Create approval record
                var approval = new Approval
                {
                    GuestInvitationId = invitation.GuestInvitationId,
                    ApprovedBy = request.ApprovedBy,
                    IsApproved = request.IsApproved,
                    Comments = request.Comments,
                    ApprovedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.ApprovedBy
                };
                await _unitOfWork.Approvals.AddAsync(approval);

                // Send notification
                if (invitation.Guest.Email != null)
                {
                    await _emailService.SendApprovalConfirmationAsync(
                        invitation.Guest.Email,
                        invitation.Guest.Name,
                        request.IsApproved,
                        request.Comments);
                }

                // Create notification events for host/creator
                var eventsToEnqueue = new List<Core.Entities.NotificationEvent>();
                var eventType = request.IsApproved ? "InvitationApproved" : "InvitationRejected";

                if (invitation.HostId.HasValue)
                {
                    var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                    var hostEvent = await _notificationEventService.CreateNotificationEventAsync(
                        eventType,
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
                        eventType,
                        invitation.GuestInvitationId,
                        invitation.CreatedBy.Value,
                        additionalData: additionalData,
                        cancellationToken: cancellationToken);
                    eventsToEnqueue.Add(creatorEvent);
                }
            }
            else
            {
                return BaseResponse<bool>.ErrorResponse("Invalid request type");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            // Enqueue events for immediate processing (after commit)
            // Note: eventsToEnqueue is scoped to the if block, so we need to re-fetch
            if (request.Type == "Invitation" || request.Type == "WalkIn")
            {
                var invitation = await _unitOfWork.GuestInvitations.GetByIdAsync(request.RequestId);
                if (invitation != null)
                {
                    var eventType = request.IsApproved ? "InvitationApproved" : "InvitationRejected";
                    var eventsToEnqueue = new List<Core.Entities.NotificationEvent>();

                    // Get events created in transaction
                    var createdEvents = await _unitOfWork.NotificationEvents
                        .FindAsync(e => e.ReferenceId == invitation.GuestInvitationId && 
                                       e.EventType == eventType && 
                                       !e.Processed);
                    
                    foreach (var evt in createdEvents)
                    {
                        eventsToEnqueue.Add(evt);
                    }

                    // Enqueue events after final commit
                    foreach (var evt in eventsToEnqueue)
                    {
                        await _eventQueue.EnqueueAsync(evt, cancellationToken);
                    }
                }
            }

            return BaseResponse<bool>.SuccessResponse(true, $"Request {(request.IsApproved ? "approved" : "rejected")} successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return BaseResponse<bool>.ErrorResponse(ex.Message);
        }
    }
}





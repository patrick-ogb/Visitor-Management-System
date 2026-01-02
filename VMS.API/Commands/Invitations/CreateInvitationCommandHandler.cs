using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text.Json;
using VMS.API.Common.Models;
using VMS.Core.Entities;
using VMS.Core.Enums;
using VMS.Infrastructure.Repositories;
using VMS.Infrastructure.Services;

namespace VMS.API.Commands.Invitations;

public class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, BaseResponse<InvitationResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IInvitationNumberService _invitationNumberService;
    private readonly INotificationEventService _notificationEventService;
    private readonly NotificationEventQueue _eventQueue;

    public CreateInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IInvitationNumberService invitationNumberService,
        INotificationEventService notificationEventService,
        NotificationEventQueue eventQueue)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _userManager = userManager;
        _invitationNumberService = invitationNumberService;
        _notificationEventService = notificationEventService;
        _eventQueue = eventQueue;
    }

    public async Task<BaseResponse<InvitationResponseDto>> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Determine host role (only for ApplicationUser type, for backward compatibility)
            bool isLfzStaff = false;
            bool isEnterpriseUser = false;
            
            if (request.HostType == HostType.ApplicationUser && request.HostId.HasValue)
            {
                // Try to get host user to determine role (backward compatibility)
                var host = await _userManager.FindByIdAsync(request.HostId.Value.ToString());
                if (host != null)
                {
                    var hostRoles = await _userManager.GetRolesAsync(host);
                    isLfzStaff = hostRoles.Contains("LFZStaff");
                    isEnterpriseUser = hostRoles.Contains("EnterpriseUser");
                }
            }

            // Combine FirstName and LastName into GuestName
            var guestName = $"{request.FirstName} {request.LastName}".Trim();

            // Create or find guest
            var existingGuest = await _unitOfWork.Guests.FirstOrDefaultAsync(g =>
                g.PhoneNumber == request.PhoneNumber &&
                (string.IsNullOrEmpty(request.Email) || g.Email == request.Email));

            Guest guest;
            if (existingGuest != null)
            {
                guest = existingGuest;
                // Update guest info if needed
                if (!string.IsNullOrEmpty(request.Email) && guest.Email != request.Email)
                {
                    guest.Email = request.Email;
                    _unitOfWork.Guests.Update(guest);
                }
                // Update name if it has changed
                if (guest.Name != guestName)
                {
                    guest.Name = guestName;
                    _unitOfWork.Guests.Update(guest);
                }
            }
            else
            {
                guest = new Guest
                {
                    Name = guestName,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                };
                await _unitOfWork.Guests.AddAsync(guest);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Determine invitation status
            var status = InvitationStatus.PendingApproval;
            
            if (request.HostType == HostType.EnterpriseUser)
            {
                // Walk-in request: Check enterprise OnePortalId for auto-approval
                if (request.EnterpriseId.HasValue)
                {
                    var enterprise = await _unitOfWork.Enterprises.GetByIdAsync(request.EnterpriseId.Value);
                    if (enterprise != null && enterprise.OnePortalId == 1)
                    {
                        // Lagos Free Zone (OnePortalId = 1) - Auto-approve
                        status = InvitationStatus.Approved;
                    }
                    // Otherwise, status remains PendingApproval
                }
            }
            else if (request.HostType == HostType.ApplicationUser)
            {
                // Regular invitation: Determine status based on host user role
                if (isLfzStaff)
                {
                    // LFZ Staff invitations are auto-approved
                    status = InvitationStatus.Approved;
                }
                // Enterprise Users require approval (default is PendingApproval)
            }
            // Default: PendingApproval

            // Generate invitation number based on year from ExpectedArrival
            // Use transaction-aware method with database locking to prevent race conditions
            var invitationYear = request.ExpectedArrival.Year;
            
            // Create invitation with retry logic for edge cases (unique constraint violations)
            GuestInvitation invitation = null!;
            int maxRetries = 3;
            bool success = false;
            string invitationNo = string.Empty;
            
            for (int attempt = 0; attempt < maxRetries && !success; attempt++)
            {
                try
                {
                    // Generate invitation number with database locking (regenerates on retries)
                    invitationNo = await _invitationNumberService.GenerateInvitationNumberWithLockAsync(_unitOfWork, invitationYear, cancellationToken);

                    invitation = new GuestInvitation
                    {
                        InvitationNo = invitationNo,
                        GuestId = guest.GuestId,
                        HostId = request.HostId,
                        EnterpriseId = request.EnterpriseId,
                        EnterpriseUserId = request.EnterpriseUserId,
                        HostName = request.HostName,
                        HostEmail = request.HostEmail,
                        HostType = request.HostType,
                        Status = status,
                        ExpectedArrival = request.ExpectedArrival,
                        ExpectedDeparture = request.ExpectedDeparture,
                        NumberOfAdditionalGuests = request.NumberOfAdditionalGuests,
                        IsProxyInvitation = request.IsProxyInvitation,
                        InvitedOnBehalfOf = request.InvitedOnBehalfOf,
                        PurposeOfInvitation = request.PurposeOfInvitation,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = request.CreatedBy
                    };

                    await _unitOfWork.GuestInvitations.AddAsync(invitation);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    success = true; // Mark as successful
                }
                catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex, "InvitationNo"))
                {
                    // Unique constraint violation on InvitationNo - retry with new number
                    if (attempt == maxRetries - 1)
                    {
                        // Last attempt failed, throw the exception
                        throw new InvalidOperationException($"Failed to generate unique invitation number after {maxRetries} attempts. Please try again.", ex);
                    }
                    // Continue to next iteration to retry
                }
            }

            if (!success || invitation == null)
            {
                throw new InvalidOperationException("Failed to create invitation after multiple retry attempts.");
            }

            // Create vehicles linked to GuestInvitation if any
            if (request.VehiclePlateNumbers != null && request.VehiclePlateNumbers.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                foreach (var plateNumber in request.VehiclePlateNumbers.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    var vehicle = new Vehicle
                    {
                        GuestInvitationId = invitation.GuestInvitationId,
                        PlateNumber = plateNumber.Trim(),
                        Model = null, // Not provided in form
                        Color = null, // Not provided in form
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = request.CreatedBy
                    };
                    await _unitOfWork.Vehicles.AddAsync(vehicle);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Track notification events to enqueue after commit
            var eventsToEnqueue = new List<Core.Entities.NotificationEvent>();

            // Send notifications based on status
            bool isWalkInRequest = request.HostType == HostType.EnterpriseUser;
            
            if (isWalkInRequest)
            {
                // Walk-in request notifications
                if (status == InvitationStatus.PendingApproval && request.EnterpriseId.HasValue)
                {
                    // Notify Enterprise Admin for walk-in requests requiring approval
                    var enterprise = await _unitOfWork.Enterprises.GetByIdAsync(request.EnterpriseId.Value);
                    if (enterprise != null)
                    {
                        var enterpriseAdmins = await _userManager.GetUsersInRoleAsync("EnterpriseAdmin");
                        var relevantAdmin = enterpriseAdmins.FirstOrDefault(a => a.EnterpriseId == request.EnterpriseId);
                        if (relevantAdmin != null && !string.IsNullOrEmpty(relevantAdmin.Email))
                        {
                            await _emailService.SendApprovalRequestNotificationAsync(
                                relevantAdmin.Email,
                                $"{relevantAdmin.FirstName} {relevantAdmin.LastName}",
                                guestName,
                                "Walk-in Request");

                            // Create notification event for EnterpriseAdmin
                            var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                            var createdEvent = await _notificationEventService.CreateNotificationEventAsync(
                                "WalkInCreated",
                                invitation.GuestInvitationId,
                                relevantAdmin.Id,
                                additionalData: additionalData,
                                cancellationToken: cancellationToken);
                            eventsToEnqueue.Add(createdEvent);
                        }
                    }
                }
                else if (status == InvitationStatus.Approved && !string.IsNullOrEmpty(guest.Email))
                {
                    // Send confirmation for auto-approved walk-in requests
                    await _emailService.SendInvitationConfirmationAsync(
                        guest.Email,
                        guest.Name,
                        request.ExpectedArrival);

                    // Create notification event for creator if they want to be notified
                    if (request.CreatedBy.HasValue)
                    {
                        var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                        var createdEvent = await _notificationEventService.CreateNotificationEventAsync(
                            "WalkInCreated",
                            invitation.GuestInvitationId,
                            request.CreatedBy.Value,
                            additionalData: additionalData,
                            cancellationToken: cancellationToken);
                        eventsToEnqueue.Add(createdEvent);
                    }
                }
            }
            else
            {
                // Regular invitation notifications
                if (status == InvitationStatus.PendingApproval && isEnterpriseUser && request.EnterpriseId.HasValue)
                {
                    // Notify Enterprise Admin
                    var enterprise = await _unitOfWork.Enterprises.GetByIdAsync(request.EnterpriseId.Value);
                    if (enterprise != null)
                    {
                        var enterpriseAdmins = await _userManager.GetUsersInRoleAsync("EnterpriseAdmin");
                        var relevantAdmin = enterpriseAdmins.FirstOrDefault(a => a.EnterpriseId == request.EnterpriseId);
                        if (relevantAdmin != null && !string.IsNullOrEmpty(relevantAdmin.Email))
                        {
                            await _emailService.SendApprovalRequestNotificationAsync(
                                relevantAdmin.Email,
                                $"{relevantAdmin.FirstName} {relevantAdmin.LastName}",
                                guestName,
                                "Invitation");

                            // Create notification event for EnterpriseAdmin
                            var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                            var createdEvent = await _notificationEventService.CreateNotificationEventAsync(
                                "InvitationCreated",
                                invitation.GuestInvitationId,
                                relevantAdmin.Id,
                                additionalData: additionalData,
                                cancellationToken: cancellationToken);
                            eventsToEnqueue.Add(createdEvent);
                        }
                    }
                }
                else if (status == InvitationStatus.Approved && !string.IsNullOrEmpty(guest.Email))
                {
                    // Send confirmation for approved invitations
                    await _emailService.SendInvitationConfirmationAsync(
                        guest.Email,
                        guest.Name,
                        request.ExpectedArrival);

                    // Create notification event for creator
                    if (request.CreatedBy.HasValue)
                    {
                        var additionalData = JsonSerializer.Serialize(new { InvitationNo = invitation.InvitationNo });
                        var createdEvent = await _notificationEventService.CreateNotificationEventAsync(
                            "InvitationCreated",
                            invitation.GuestInvitationId,
                            request.CreatedBy.Value,
                            additionalData: additionalData,
                            cancellationToken: cancellationToken);
                        eventsToEnqueue.Add(createdEvent);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            // Enqueue events after transaction commit for immediate processing
            foreach (var evt in eventsToEnqueue)
            {
                await _eventQueue.EnqueueAsync(evt, cancellationToken);
            }

            var response = new InvitationResponseDto
            {
                GuestInvitationId = invitation.GuestInvitationId,
                InvitationNo = invitation.InvitationNo,
                GuestId = guest.GuestId,
                GuestName = guest.Name,
                Status = status.ToString(),
                ExpectedArrival = invitation.ExpectedArrival,
                ExpectedDeparture = invitation.ExpectedDeparture
            };

            return BaseResponse<InvitationResponseDto>.SuccessResponse(response, "Invitation created successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return BaseResponse<InvitationResponseDto>.ErrorResponse(ex.Message);
        }
    }

    /// <summary>
    /// Checks if a DbUpdateException is due to a unique constraint violation on a specific column
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex, string columnName)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            // SQL Server unique constraint violation error code
            // 2601 = Cannot insert duplicate key row in object with unique index
            // 2627 = Violation of UNIQUE KEY constraint
            if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
            {
                // Check if the error message mentions the column name
                var errorMessage = sqlEx.Message;
                return errorMessage.Contains(columnName, StringComparison.OrdinalIgnoreCase) ||
                       errorMessage.Contains("InvitationNo", StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }
}



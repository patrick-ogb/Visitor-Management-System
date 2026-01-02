using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using VMS.API.Common.Models;
using VMS.Core.Entities;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Repositories;
using VMS.Infrastructure.Services;

namespace VMS.API.Commands.Gate;

public class CreateWalkInRequestCommandHandler : IRequestHandler<CreateWalkInRequestCommand, BaseResponse<WalkInRequestResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly VmsDbContext _context;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IInvitationNumberService _invitationNumberService;

    public CreateWalkInRequestCommandHandler(
        IUnitOfWork unitOfWork,
        VmsDbContext context,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager,
        IInvitationNumberService invitationNumberService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _emailService = emailService;
        _userManager = userManager;
        _invitationNumberService = invitationNumberService;
    }

    public async Task<BaseResponse<WalkInRequestResponseDto>> Handle(CreateWalkInRequestCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Find HostId from EnterpriseUserId
            int hostId;
            if (request.EnterpriseUserId.HasValue)
            {
                var enterpriseUser = await _context.EnterpriseUsers.FindAsync(new object[] { request.EnterpriseUserId.Value }, cancellationToken);
                if (enterpriseUser == null)
                {
                    return BaseResponse<WalkInRequestResponseDto>.ErrorResponse("Selected host/employee not found");
                }

                // Find ApplicationUser by matching email
                var applicationUser = await _userManager.FindByEmailAsync(enterpriseUser.EmailAddress);
                if (applicationUser == null)
                {
                    return BaseResponse<WalkInRequestResponseDto>.ErrorResponse($"No application user found for email {enterpriseUser.EmailAddress}. The enterprise user must have a corresponding application user account.");
                }

                hostId = applicationUser.Id;
            }
            else
            {
                return BaseResponse<WalkInRequestResponseDto>.ErrorResponse("Host/Employee is required");
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

            // Determine status based on enterprise OnePortalId
            InvitationStatus status = InvitationStatus.PendingApproval; // Default to pending approval
            
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
                    // Generate invitation number (with locking on first attempt, regenerate on retries)
                    invitationNo = await _invitationNumberService.GenerateInvitationNumberWithLockAsync(_unitOfWork, invitationYear, cancellationToken);

                    invitation = new GuestInvitation
                    {
                        InvitationNo = invitationNo,
                        GuestId = guest.GuestId,
                        HostId = hostId,
                        EnterpriseId = request.EnterpriseId,
                        Status = status,
                        ExpectedArrival = request.ExpectedArrival,
                        ExpectedDeparture = request.ExpectedDeparture,
                        NumberOfAdditionalGuests = request.NumberOfGuests,
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
                throw new InvalidOperationException("Failed to create walk-in request after multiple retry attempts.");
            }

            // Create vehicles linked to GuestInvitation if vehicle plate numbers provided
            if (!string.IsNullOrWhiteSpace(request.VehiclePlateNumber))
            {
                // Split comma-separated plate numbers
                var plateNumbers = request.VehiclePlateNumber.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var plateNumber in plateNumbers)
                {
                    if (!string.IsNullOrWhiteSpace(plateNumber))
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
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Only send approval notification emails if status is PendingApproval
            if (status == InvitationStatus.PendingApproval && request.EnterpriseId.HasValue)
            {
                // Route to Enterprise Admin
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
            }

            await _unitOfWork.CommitTransactionAsync();

            var response = new WalkInRequestResponseDto
            {
                GuestInvitationId = invitation.GuestInvitationId,
                InvitationNo = invitation.InvitationNo,
                GuestName = guest.Name,
                Status = invitation.Status.ToString(),
                ExpectedArrival = invitation.ExpectedArrival
            };

            return BaseResponse<WalkInRequestResponseDto>.SuccessResponse(response, "Walk-in request created successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return BaseResponse<WalkInRequestResponseDto>.ErrorResponse(ex.Message);
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





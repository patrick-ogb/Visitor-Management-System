using MediatR;
using VMS.API.Common.Models;
using VMS.Core.Enums;

namespace VMS.API.Commands.Invitations;

public class CreateInvitationCommand : IRequest<BaseResponse<InvitationResponseDto>>
{
    // Guest Details
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    
    // Visit Details
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
    
    // Proxy Invitation
    public bool IsProxyInvitation { get; set; }
    public string? InvitedOnBehalfOf { get; set; }
    
    // Group & Vehicle
    public int NumberOfAdditionalGuests { get; set; } = 0;
    public int NumberOfVehicles { get; set; } = 0;
    public List<string> VehiclePlateNumbers { get; set; } = new();
    public List<string>? VehicleModels { get; set; }
    public List<string>? VehicleColors { get; set; }
    
    // Purpose
    public string PurposeOfInvitation { get; set; } = string.Empty;
    
    // Host info
    public int? HostId { get; set; } // ApplicationUser ID (optional)
    public int? EnterpriseId { get; set; }
    public int? EnterpriseUserId { get; set; } // EnterpriseUser ID for walk-in requests
    
    // Host information (stored directly)
    public string HostName { get; set; } = string.Empty; // Required
    public string? HostEmail { get; set; } // Optional
    public HostType HostType { get; set; } = HostType.ApplicationUser;
    
    // CreatedBy (set by controller from authenticated user)
    public int? CreatedBy { get; set; }
}

public class InvitationResponseDto
{
    public int GuestInvitationId { get; set; }
    public string InvitationNo { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
}



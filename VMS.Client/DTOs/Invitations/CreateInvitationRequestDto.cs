namespace VMS.Client.DTOs.Invitations;

public class CreateInvitationRequestDto
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
    public List<string> VehiclePlateNumbers { get; set; } = new();
    
    // Purpose
    public string PurposeOfInvitation { get; set; } = string.Empty;
    
    // Host/Enterprise (for walk-in requests)
    public int? EnterpriseId { get; set; }
    public int? EnterpriseUserId { get; set; }
}


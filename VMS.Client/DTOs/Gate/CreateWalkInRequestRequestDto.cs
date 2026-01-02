namespace VMS.Client.DTOs.Gate;

public class CreateWalkInRequestRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int? EnterpriseId { get; set; }
    public int? EnterpriseUserId { get; set; } // ID of the selected EnterpriseUser from dropdown
    public string? VehiclePlateNumber { get; set; }
    public int NumberOfGuests { get; set; } = 1;
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
    public string? PurposeOfInvitation { get; set; }
}

public class WalkInRequestResponseDto
{
    public int GuestInvitationId { get; set; }
    public string InvitationNo { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpectedArrival { get; set; }
}


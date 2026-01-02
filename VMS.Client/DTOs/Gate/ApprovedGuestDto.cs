namespace VMS.Client.DTOs.Gate;

public class ApprovedGuestDto
{
    public int GuestInvitationId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? EnterpriseName { get; set; }
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
    public int NumberOfAdditionalGuests { get; set; }
    public string? PurposeOfInvitation { get; set; }
    public List<VehicleDto> Vehicles { get; set; } = new();
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}



namespace VMS.Client.DTOs.Invitations;

public class InvitationDto
{
    public int GuestInvitationId { get; set; }
    public string InvitationNo { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string? GuestEmail { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? HostEmail { get; set; }
    public string? EnterpriseName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
    public int NumberOfAdditionalGuests { get; set; }
    public bool IsProxyInvitation { get; set; }
    public string? InvitedOnBehalfOf { get; set; }
    public string? PurposeOfInvitation { get; set; }
    public List<VehicleDto> Vehicles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool IsRescheduled { get; set; }
    public int? RescheduledById { get; set; }
    public DateTime? RescheduledAt { get; set; }
}

public class VehicleDto
{
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Color { get; set; }
}


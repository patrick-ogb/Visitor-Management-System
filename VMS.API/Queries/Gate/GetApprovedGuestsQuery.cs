using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Gate;

public class GetApprovedGuestsQuery : IRequest<BaseResponse<List<ApprovedGuestDto>>>
{
    public DateTime? Date { get; set; }
}

public class ApprovedGuestDto
{
    public int GuestInvitationId { get; set; }
    public string InvitationNo { get; set; } = string.Empty;
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

public class VehicleDto
{
    public string PlateNumber { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Color { get; set; }
}





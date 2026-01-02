using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Gate;

public class SearchGuestQuery : IRequest<BaseResponse<SearchGuestResultDto>>
{
    public string? Name { get; set; }
    public string? InvitationNo { get; set; }
    public string? VehiclePlateNumber { get; set; }
    public string? VehicleModel { get; set; }
    public string? VehicleColor { get; set; }
}

public class SearchGuestResultDto
{
    public bool Found { get; set; }
    public bool IsApproved { get; set; }
    public ApprovedGuestDto? Guest { get; set; }
}





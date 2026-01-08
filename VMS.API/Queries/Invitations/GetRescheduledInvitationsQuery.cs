using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Invitations;

public class GetRescheduledInvitationsQuery : IRequest<BaseResponse<PagedResponse<InvitationDto>>>
{
    public int? HostId { get; set; }
    public string? Status { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}


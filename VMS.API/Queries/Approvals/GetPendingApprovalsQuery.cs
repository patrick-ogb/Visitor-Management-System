using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Approvals;

public class GetPendingApprovalsQuery : IRequest<BaseResponse<List<PendingApprovalDto>>>
{
    public int ApproverId { get; set; }
}

public class PendingApprovalDto
{
    public int ApprovalId { get; set; }
    public string Type { get; set; } = string.Empty; // "Invitation" or "WalkIn"
    public int? GuestInvitationId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? EnterpriseName { get; set; }
    public DateTime ExpectedArrival { get; set; }
    public DateTime ExpectedDeparture { get; set; }
    public DateTime RequestedAt { get; set; }
}





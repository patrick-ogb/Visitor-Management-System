using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Approvals;

public class GetPendingApprovalsQueryHandler : IRequestHandler<GetPendingApprovalsQuery, BaseResponse<List<PendingApprovalDto>>>
{
    private readonly VmsDbContext _context;

    public GetPendingApprovalsQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<PendingApprovalDto>>> Handle(GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var approvals = new List<PendingApprovalDto>();

        // Get pending invitations for Enterprise Admin
        var pendingInvitations = await _context.GuestInvitations
            .Include(i => i.Guest)
            .Include(i => i.Enterprise)
            .Where(i => i.Status == InvitationStatus.PendingApproval &&
                       i.EnterpriseId.HasValue &&
                       i.Enterprise!.Users.Any(u => u.Id == request.ApproverId))
            .ToListAsync(cancellationToken);

        approvals.AddRange(pendingInvitations.Select(i => new PendingApprovalDto
        {
            ApprovalId = 0, // Will be set when approval is created
            Type = "Invitation",
            GuestInvitationId = i.GuestInvitationId,
            GuestName = i.Guest.Name,
            PhoneNumber = i.Guest.PhoneNumber,
            Email = i.Guest.Email,
            HostName = i.HostName,
            EnterpriseName = i.Enterprise?.Name,
            ExpectedArrival = i.ExpectedArrival,
            ExpectedDeparture = i.ExpectedDeparture,
            RequestedAt = i.CreatedAt
        }));

        // Walk-in requests are now GuestInvitations, so they're already included in the query above

        return BaseResponse<List<PendingApprovalDto>>.SuccessResponse(approvals.OrderByDescending(a => a.RequestedAt).ToList());
    }
}


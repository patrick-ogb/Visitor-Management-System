using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Dashboard;

public class GetDashboardMetricsQueryHandler : IRequestHandler<GetDashboardMetricsQuery, BaseResponse<DashboardMetricsDto>>
{
    private readonly VmsDbContext _context;

    public GetDashboardMetricsQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<DashboardMetricsDto>> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Base query for invitations
            var invitationsQuery = _context.GuestInvitations.AsQueryable();

            // Filter by enterprise if specified
            if (request.EnterpriseId.HasValue)
            {
                invitationsQuery = invitationsQuery.Where(i => i.EnterpriseId == request.EnterpriseId.Value);
            }

            // Total Visitors: Count of all checked-in invitations
            var totalVisitors = await invitationsQuery
                .CountAsync(i => i.Status == InvitationStatus.CheckedIn, cancellationToken);

            // Total Enterprises: Count of distinct enterprises
            int totalEnterprises;
            if (request.EnterpriseId.HasValue)
            {
                // If filtering by enterprise, only count that one enterprise
                totalEnterprises = 1;
            }
            else
            {
                totalEnterprises = await _context.Enterprises
                    .CountAsync(cancellationToken);
            }

            // Walk-In Requests: Count of invitations where HostType = EnterpriseUser
            var walkInRequests = await invitationsQuery
                .CountAsync(i => i.HostType == HostType.EnterpriseUser, cancellationToken);

            var metrics = new DashboardMetricsDto
            {
                TotalVisitors = totalVisitors,
                TotalEnterprises = totalEnterprises,
                WalkInRequests = walkInRequests
            };

            return BaseResponse<DashboardMetricsDto>.SuccessResponse(metrics);
        }
        catch (Exception ex)
        {
            return BaseResponse<DashboardMetricsDto>.ErrorResponse($"Error retrieving dashboard metrics: {ex.Message}");
        }
    }
}


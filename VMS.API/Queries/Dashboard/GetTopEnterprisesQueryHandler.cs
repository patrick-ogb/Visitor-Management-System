using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Dashboard;

public class GetTopEnterprisesQueryHandler : IRequestHandler<GetTopEnterprisesQuery, BaseResponse<List<TopEnterpriseDto>>>
{
    private readonly VmsDbContext _context;

    public GetTopEnterprisesQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<TopEnterpriseDto>>> Handle(GetTopEnterprisesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Base query for checked-in invitations
            var invitationsQuery = _context.GuestInvitations
                .Where(i => i.Status == InvitationStatus.CheckedIn && i.EnterpriseId.HasValue);

            // Filter by enterprise if specified (for non-SUPERADMIN users)
            if (request.EnterpriseId.HasValue)
            {
                invitationsQuery = invitationsQuery.Where(i => i.EnterpriseId == request.EnterpriseId.Value);
            }

            // Group by EnterpriseId and count visitors
            var enterpriseCounts = await invitationsQuery
                .GroupBy(i => i.EnterpriseId!.Value)
                .Select(g => new
                {
                    EnterpriseId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(request.Count)
                .ToListAsync(cancellationToken);

            // Get total count for percentage calculation
            var totalCount = enterpriseCounts.Sum(x => x.Count);

            // Get enterprise names
            var enterpriseIds = enterpriseCounts.Select(x => x.EnterpriseId).ToList();
            var enterprises = await _context.Enterprises
                .Where(e => enterpriseIds.Contains(e.EnterpriseId))
                .Select(e => new { e.EnterpriseId, e.Name })
                .ToListAsync(cancellationToken);

            // Build result with percentages
            var result = enterpriseCounts
                .Select(ec =>
                {
                    var enterprise = enterprises.FirstOrDefault(e => e.EnterpriseId == ec.EnterpriseId);
                    return new TopEnterpriseDto
                    {
                        Name = enterprise?.Name ?? "Unknown",
                        Count = ec.Count,
                        Percentage = totalCount > 0 ? Math.Round((double)ec.Count / totalCount * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            return BaseResponse<List<TopEnterpriseDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<TopEnterpriseDto>>.ErrorResponse($"Error retrieving top enterprises: {ex.Message}");
        }
    }
}




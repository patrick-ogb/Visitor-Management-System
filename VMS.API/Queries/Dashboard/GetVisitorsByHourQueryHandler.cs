using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Dashboard;

public class GetVisitorsByHourQueryHandler : IRequestHandler<GetVisitorsByHourQuery, BaseResponse<List<VisitorsByHourDto>>>
{
    private readonly VmsDbContext _context;

    public GetVisitorsByHourQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<VisitorsByHourDto>>> Handle(GetVisitorsByHourQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var targetDate = request.Date?.Date ?? DateTime.Today;

            // Query invitations that were checked in on the target date
            var query = _context.GuestInvitations
                .Where(i => i.CheckedInAt.HasValue &&
                           i.CheckedInAt.Value.Date == targetDate &&
                           i.Status == InvitationStatus.CheckedIn);

            // Filter by enterprise if specified
            if (request.EnterpriseId.HasValue)
            {
                query = query.Where(i => i.EnterpriseId == request.EnterpriseId.Value);
            }

            // Group by hour and count
            var visitorsByHour = await query
                .GroupBy(i => i.CheckedInAt!.Value.Hour)
                .Select(g => new VisitorsByHourDto
                {
                    Hour = g.Key,
                    Count = g.Count(),
                    HourLabel = $"{g.Key:D2}:00"
                })
                .OrderBy(x => x.Hour)
                .ToListAsync(cancellationToken);

            // Ensure all 24 hours are represented (fill in missing hours with 0)
            var allHours = Enumerable.Range(0, 24)
                .Select(hour => new VisitorsByHourDto
                {
                    Hour = hour,
                    Count = visitorsByHour.FirstOrDefault(v => v.Hour == hour)?.Count ?? 0,
                    HourLabel = $"{hour:D2}:00"
                })
                .ToList();

            return BaseResponse<List<VisitorsByHourDto>>.SuccessResponse(allHours);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<VisitorsByHourDto>>.ErrorResponse($"Error retrieving visitors by hour: {ex.Message}");
        }
    }
}



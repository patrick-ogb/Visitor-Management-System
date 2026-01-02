using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Dashboard;

public class GetVisitorsByDayQueryHandler : IRequestHandler<GetVisitorsByDayQuery, BaseResponse<List<VisitorsByDayDto>>>
{
    private readonly VmsDbContext _context;

    public GetVisitorsByDayQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<VisitorsByDayDto>>> Handle(GetVisitorsByDayQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Default to last 7 days if no date range specified
            var fromDate = request.FromDate?.Date ?? DateTime.Today.AddDays(-6);
            var toDate = request.ToDate?.Date ?? DateTime.Today;

            // Query invitations that were checked in within the date range
            var query = _context.GuestInvitations
                .Where(i => i.CheckedInAt.HasValue &&
                           i.CheckedInAt.Value.Date >= fromDate &&
                           i.CheckedInAt.Value.Date <= toDate &&
                           i.Status == InvitationStatus.CheckedIn);

            // Filter by enterprise if specified
            if (request.EnterpriseId.HasValue)
            {
                query = query.Where(i => i.EnterpriseId == request.EnterpriseId.Value);
            }

            // Group by day of week and count
            var visitorsByDay = await query
                .GroupBy(i => i.CheckedInAt!.Value.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            // Map DayOfWeek enum to abbreviated day names
            var dayNameMap = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Mon" },
                { DayOfWeek.Tuesday, "Tue" },
                { DayOfWeek.Wednesday, "Wed" },
                { DayOfWeek.Thursday, "Thur" },
                { DayOfWeek.Friday, "Fri" },
                { DayOfWeek.Saturday, "Sat" },
                { DayOfWeek.Sunday, "Sun" }
            };

            // Convert to DTOs and ensure all days are represented
            var allDays = new List<VisitorsByDayDto>();
            var dayOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };

            foreach (var day in dayOrder)
            {
                var dayData = visitorsByDay.FirstOrDefault(v => v.DayOfWeek == day);
                allDays.Add(new VisitorsByDayDto
                {
                    Day = dayNameMap[day],
                    Count = dayData?.Count ?? 0
                });
            }

            return BaseResponse<List<VisitorsByDayDto>>.SuccessResponse(allDays);
        }
        catch (Exception ex)
        {
            return BaseResponse<List<VisitorsByDayDto>>.ErrorResponse($"Error retrieving visitors by day: {ex.Message}");
        }
    }
}


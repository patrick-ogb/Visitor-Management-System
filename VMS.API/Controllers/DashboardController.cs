using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Queries.Dashboard;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,EnterpriseAdmin,LFZAdmin")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("visitors-by-hour")]
    public async Task<ActionResult> GetVisitorsByHour([FromQuery] DateTime? date, [FromQuery] int? enterpriseId)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        var isSuperadmin = User.IsInRole("SUPERADMIN");

        var query = new GetVisitorsByHourQuery
        {
            Date = date,
            EnterpriseId = enterpriseId
        };

        // Auto-filter by enterprise for non-SUPERADMIN users
        if (!isSuperadmin)
        {
            var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
            if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var userEnterpriseId))
            {
                // Only set if query doesn't already have EnterpriseId specified
                if (!query.EnterpriseId.HasValue)
                {
                    query.EnterpriseId = userEnterpriseId;
                }
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult> GetDashboardMetrics()
    {
        var isSuperadmin = User.IsInRole("SUPERADMIN");

        var query = new GetDashboardMetricsQuery();

        // Auto-filter by enterprise for non-SUPERADMIN users
        if (!isSuperadmin)
        {
            var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
            if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var userEnterpriseId))
            {
                query.EnterpriseId = userEnterpriseId;
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("visitors-by-day")]
    public async Task<ActionResult> GetVisitorsByDay([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? viewMode)
    {
        var isSuperadmin = User.IsInRole("SUPERADMIN");

        // Parse viewMode parameter (default to ByDate)
        VisitorsByDayViewMode parsedViewMode = VisitorsByDayViewMode.ByDate;
        if (!string.IsNullOrWhiteSpace(viewMode) && Enum.TryParse<VisitorsByDayViewMode>(viewMode, true, out var parsed))
        {
            parsedViewMode = parsed;
        }

        var query = new GetVisitorsByDayQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            ViewMode = parsedViewMode
        };

        // Auto-filter by enterprise for non-SUPERADMIN users
        if (!isSuperadmin)
        {
            var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
            if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var userEnterpriseId))
            {
                query.EnterpriseId = userEnterpriseId;
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("top-enterprises")]
    public async Task<ActionResult> GetTopEnterprises([FromQuery] int count = 5)
    {
        var isSuperadmin = User.IsInRole("SUPERADMIN");

        var query = new GetTopEnterprisesQuery
        {
            Count = count
        };

        // Auto-filter by enterprise for non-SUPERADMIN users
        if (!isSuperadmin)
        {
            var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
            if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var userEnterpriseId))
            {
                query.EnterpriseId = userEnterpriseId;
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}




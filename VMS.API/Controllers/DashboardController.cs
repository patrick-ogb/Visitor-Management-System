using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Queries.Dashboard;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,EnterpriseAdmin")]
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
}



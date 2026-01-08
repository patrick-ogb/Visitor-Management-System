using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Queries.Invitations;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,LFZUser,EnterpriseUser")]
public class RescheduledInvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RescheduledInvitationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult> GetRescheduledInvitations([FromQuery] GetRescheduledInvitationsQuery query)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        // Auto-filter by current user's HostId (they can only see their own rescheduled invitations)
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            // Only set if query doesn't already have HostId specified
            if (!query.HostId.HasValue)
            {
                query.HostId = userId;
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}


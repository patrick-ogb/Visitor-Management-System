using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Commands.Gate;
using VMS.API.Queries.Gate;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,GateAdmin")]
public class GateController : ControllerBase
{
    private readonly IMediator _mediator;

    public GateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("approved-guests")]
    public async Task<ActionResult> GetApprovedGuests([FromQuery] GetApprovedGuestsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("search")]
    public async Task<ActionResult> SearchGuest([FromBody] SearchGuestQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    // Walk-in endpoint removed - now handled by InvitationsController
    // Walk-in requests are now created as GuestInvitation entries via api/Invitations

    [HttpPut("check-in/{id}")]
    public async Task<ActionResult> CheckInGuest(int id)
    {
        var command = new CheckInGuestCommand { GuestInvitationId = id };
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpPut("reject/{id}")]
    public async Task<ActionResult> RejectGuest(int id)
    {
        var command = new RejectGuestCommand { GuestInvitationId = id };
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpGet("checked-in-guests")]
    public async Task<ActionResult> GetCheckedInGuests()
    {
        var query = new GetCheckedInGuestsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("check-out/{id}")]
    public async Task<ActionResult> CheckOutGuest(int id)
    {
        var command = new CheckOutGuestCommand { GuestInvitationId = id };
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpPost("reschedule-invitation")]
    public async Task<ActionResult> RescheduleInvitation([FromBody] RescheduleGuestInvitationCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }
}





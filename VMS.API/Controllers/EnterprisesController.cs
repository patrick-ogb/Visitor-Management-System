using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Commands.Enterprises;
using VMS.API.Queries.Enterprises;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,LFZAdmin,EnterpriseAdmin,GateAdmin")]
public class EnterprisesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnterprisesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult> GetEnterprises([FromQuery] GetEnterprisesQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("sync")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult> SyncEnterprises()
    {
        var command = new SyncEnterprisesCommand();
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }
}


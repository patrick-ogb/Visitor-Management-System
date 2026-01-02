using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Commands.EnterpriseUsers;
using VMS.API.Queries.EnterpriseUsers;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,LFZAdmin,EnterpriseAdmin,GateAdmin")]
public class EnterpriseUsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnterpriseUsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("by-enterprise/{enterpriseId}")]
    public async Task<ActionResult> GetEnterpriseUsersByEnterprise(int enterpriseId)
    {
        var query = new GetEnterpriseUsersByEnterpriseQuery
        {
            EnterpriseId = enterpriseId
        };
        var result = await _mediator.Send(query);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpPost("sync/{onePortalEnterpriseId}")]
    [Authorize(Roles = "SUPERADMIN")]
    public async Task<ActionResult> SyncEnterpriseUsers(int onePortalEnterpriseId)
    {
        var command = new SyncEnterpriseUsersCommand
        {
            OnePortalEnterpriseId = onePortalEnterpriseId
        };
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }
}


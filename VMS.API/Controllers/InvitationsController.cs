using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Commands.Invitations;
using VMS.API.Queries.Invitations;
using VMS.Core.Entities;
using VMS.Core.Enums;
using VMS.Infrastructure.Data;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,LFZStaff,EnterpriseUser,EnterpriseAdmin,GateAdmin")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly VmsDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InvitationsController(
        IMediator mediator,
        VmsDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _mediator = mediator;
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<ActionResult> CreateInvitation([FromBody] CreateInvitationCommand command)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var isSuperadmin = User.IsInRole("SUPERADMIN");
        
        // Handle EnterpriseUserId mapping for walk-in requests (from GateAdmin)
        if (command.EnterpriseUserId.HasValue)
        {
            // Find EnterpriseUser by ID
            var enterpriseUser = await _context.EnterpriseUsers.FindAsync(new object[] { command.EnterpriseUserId.Value });
            if (enterpriseUser == null)
            {
                return BadRequest(new { message = "Selected host/employee not found" });
            }

            // Populate host information from EnterpriseUser
            command.HostName = enterpriseUser.FullName;
            command.HostEmail = enterpriseUser.EmailAddress;
            command.HostType = HostType.EnterpriseUser;
            command.EnterpriseUserId = enterpriseUser.EnterpriseUserId;
            command.HostId = null; // No ApplicationUser required for walk-in requests
            
            // Keep EnterpriseId as-is (already set from selected enterprise dropdown)
            // Don't override EnterpriseId from user claims for walk-in requests
        }
        else
        {
            // Regular invitation flow
            // Only auto-set EnterpriseId if not SUPERADMIN and not already provided
            if (!isSuperadmin)
            {
                var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
                if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var enterpriseId))
                {
                    // Only set if command doesn't already have EnterpriseId specified
                    if (!command.EnterpriseId.HasValue)
                    {
                        command.EnterpriseId = enterpriseId;
                    }
                }
            }

            // Set HostId if not already provided (SUPERADMIN can override)
            if (!command.HostId.HasValue)
            {
                command.HostId = userId;
            }

            // Populate host information from ApplicationUser
            if (command.HostId.HasValue)
            {
                var applicationUser = await _userManager.FindByIdAsync(command.HostId.Value.ToString());
                if (applicationUser != null)
                {
                    command.HostName = $"{applicationUser.FirstName} {applicationUser.LastName}".Trim();
                    command.HostEmail = applicationUser.Email;
                    command.HostType = HostType.ApplicationUser;
                }
                else
                {
                    // Fallback: use authenticated user info if HostId user not found
                    var currentUser = await _userManager.FindByIdAsync(userId.ToString());
                    if (currentUser != null)
                    {
                        command.HostName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                        command.HostEmail = currentUser.Email;
                        command.HostType = HostType.ApplicationUser;
                        command.HostId = userId;
                    }
                    else
                    {
                        return BadRequest(new { message = "Unable to determine host information" });
                    }
                }
            }
            else
            {
                // Use authenticated user as host
                var currentUser = await _userManager.FindByIdAsync(userId.ToString());
                if (currentUser != null)
                {
                    command.HostName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                    command.HostEmail = currentUser.Email;
                    command.HostType = HostType.ApplicationUser;
                    command.HostId = userId;
                }
                else
                {
                    return BadRequest(new { message = "Unable to determine host information" });
                }
            }
        }
        
        // Set CreatedBy from authenticated user
        command.CreatedBy = userId;
        
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpGet]
    public async Task<ActionResult> GetInvitations([FromQuery] GetInvitationsQuery query)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        var isSuperadmin = User.IsInRole("SUPERADMIN");
        var isEnterpriseAdmin = User.IsInRole("EnterpriseAdmin");
        
        // Only auto-filter by HostId if not SUPERADMIN and not EnterpriseAdmin and not already specified
        // EnterpriseAdmin should see all invitations for their enterprise (not filtered by HostId)
        if (!isSuperadmin && !isEnterpriseAdmin)
        {
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                // Only set if query doesn't already have HostId specified
                if (!query.HostId.HasValue)
                {
                    query.HostId = userId;
                }
            }
        }

        // Set EnterpriseId for non-SUPERADMIN users (including EnterpriseAdmin)
        if (!isSuperadmin)
        {
            var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
            if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var enterpriseId))
            {
                // Only set if query doesn't already have EnterpriseId specified
                if (!query.EnterpriseId.HasValue)
                {
                    query.EnterpriseId = enterpriseId;
                }
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("cancel/{id}")]
    public async Task<ActionResult> CancelInvitation(int id)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var command = new CancelInvitationCommand 
        { 
            GuestInvitationId = id,
            CreatedBy = userId
        };
        
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }
}



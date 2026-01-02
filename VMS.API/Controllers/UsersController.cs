using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.Commands.Users;
using VMS.API.Queries.Users;
using VMS.API.Common.Models;
using VMS.Infrastructure.Services.Authentication;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SUPERADMIN,LFZAdmin,EnterpriseAdmin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOnePortalApiService _onePortalApiService;

    public UsersController(IMediator mediator, IOnePortalApiService onePortalApiService)
    {
        _mediator = mediator;
        _onePortalApiService = onePortalApiService;
    }

    [HttpGet]
    public async Task<ActionResult> GetUsers([FromQuery] GetUsersQuery query)
    {
        // Filter by enterprise for EnterpriseAdmin users
        var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
        var isSuperadmin = User.IsInRole("SUPERADMIN");

        if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var enterpriseId))
        {
            // EnterpriseAdmin can only see users from their enterprise
            var isEnterpriseAdmin = User.IsInRole("EnterpriseAdmin");
            if (isEnterpriseAdmin && !User.IsInRole("LFZAdmin") && !isSuperadmin)
            {
                query.EnterpriseId = enterpriseId;
            }
        }

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // EnterpriseAdmin can only create users for their own enterprise
        var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
        if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var enterpriseId))
        {
            var isEnterpriseAdmin = User.IsInRole("EnterpriseAdmin");
            if (isEnterpriseAdmin && !User.IsInRole("LFZAdmin") && !User.IsInRole("SUPERADMIN"))
            {
                // EnterpriseAdmin can only create EnterpriseUser role
                if (command.Role != "EnterpriseUser")
                {
                    return Forbid("EnterpriseAdmin can only create EnterpriseUser accounts");
                }
                command.EnterpriseId = enterpriseId;
            }
        }

        command.CreatedBy = userId;
        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return CreatedAtAction(nameof(GetUsers), result.Data);
        }
        
        return BadRequest(result);
    }

    [HttpPut("{userId}/role")]
    public async Task<ActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleCommand command)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        command.UserId = userId;

        // EnterpriseAdmin can only update users from their own enterprise
        var enterpriseIdClaim = User.FindFirst("enterpriseId")?.Value;
        if (!string.IsNullOrEmpty(enterpriseIdClaim) && int.TryParse(enterpriseIdClaim, out var enterpriseId))
        {
            var isEnterpriseAdmin = User.IsInRole("EnterpriseAdmin");
            if (isEnterpriseAdmin && !User.IsInRole("LFZAdmin"))
            {
                // EnterpriseAdmin can only assign EnterpriseUser role
                if (command.NewRole != "EnterpriseUser")
                {
                    return Forbid("EnterpriseAdmin can only assign EnterpriseUser role");
                }
                command.EnterpriseId = enterpriseId;
            }
        }

        var result = await _mediator.Send(command);
        
        if (result.Success)
        {
            return Ok(result);
        }
        
        return BadRequest(result);
    }

    [HttpGet("enterprises")]
    public async Task<ActionResult<BaseResponse<List<Queries.Enterprises.EnterpriseDto>>>> GetEnterprises()
    {
        // Extract OnePortal token from JWT claims
        var onePortalToken = User.FindFirst("onePortalToken")?.Value;

        if (string.IsNullOrWhiteSpace(onePortalToken))
        {
            // If no OnePortal token, user logged in via Identity - return empty list or error
            return Ok(BaseResponse<List<Queries.Enterprises.EnterpriseDto>>.SuccessResponse(
                new List<Queries.Enterprises.EnterpriseDto>(),
                "No OnePortal token found. User may have logged in via Identity authentication."));
        }

        var result = await _onePortalApiService.GetEnterprisesAsync(onePortalToken);

        if (!result.Success)
        {
            return BadRequest(BaseResponse<List<Queries.Enterprises.EnterpriseDto>>.ErrorResponse(
                result.Message ?? "Failed to load enterprises from OnePortal",
                result.Errors));
        }

        // Map Core.DTOs.EnterpriseDto to API.Queries.Enterprises.EnterpriseDto
        var enterprises = result.Data?.Select(e => new Queries.Enterprises.EnterpriseDto
        {
            EnterpriseId = e.EnterpriseId,
            OnePortalId = e.OnePortalId,
            Code = e.Code,
            Name = e.Name,
            Address = e.Address,
            ContactEmail = e.ContactEmail,
            ContactPhone = e.ContactPhone,
            IsActive = e.IsActive
        }).ToList() ?? new List<Queries.Enterprises.EnterpriseDto>();

        return Ok(BaseResponse<List<Queries.Enterprises.EnterpriseDto>>.SuccessResponse(enterprises));
    }

    [HttpGet("by-enterprise/{enterpriseId}")]
    public async Task<ActionResult<BaseResponse<List<Queries.Users.UserDto>>>> GetUsersByEnterprise(int enterpriseId)
    {
        // Extract OnePortal token from JWT claims
        var onePortalToken = User.FindFirst("onePortalToken")?.Value;

        if (string.IsNullOrWhiteSpace(onePortalToken))
        {
            // If no OnePortal token, user logged in via Identity - return empty list or error
            return Ok(BaseResponse<List<Queries.Users.UserDto>>.SuccessResponse(
                new List<Queries.Users.UserDto>(),
                "No OnePortal token found. User may have logged in via Identity authentication."));
        }

        var result = await _onePortalApiService.GetUsersByEnterpriseAsync(enterpriseId, onePortalToken);

        if (!result.Success)
        {
            return BadRequest(BaseResponse<List<Queries.Users.UserDto>>.ErrorResponse(
                result.Message ?? "Failed to load users from OnePortal",
                result.Errors));
        }

        // Map Core.DTOs.UserDto to API.Queries.Users.UserDto
        var users = result.Data?.Select(u => new Queries.Users.UserDto
        {
            UserId = u.UserId,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Phone = u.Phone,
            Roles = u.Roles,
            EnterpriseId = u.EnterpriseId,
            EnterpriseName = u.EnterpriseName,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList() ?? new List<Queries.Users.UserDto>();

        return Ok(BaseResponse<List<Queries.Users.UserDto>>.SuccessResponse(users));
    }
}



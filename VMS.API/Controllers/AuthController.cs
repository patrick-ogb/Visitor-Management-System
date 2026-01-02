using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VMS.API.DTOs.Auth;
using VMS.Core.Services.Authentication;
using VMS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using VMS.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace VMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(
        IAuthenticationService authenticationService,
        IUserService userService,
        UserManager<ApplicationUser> userManager)
    {
        _authenticationService = authenticationService;
        _userService = userService;
        _userManager = userManager;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var result = await _authenticationService.AuthenticateAsync(request.Email, request.Password);
            
            var response = new AuthResponseDto
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserInfo = new UserInfoDto
                {
                    UserId = result.UserInfo.UserId,
                    Email = result.UserInfo.Email,
                    FirstName = result.UserInfo.FirstName,
                    LastName = result.UserInfo.LastName,
                    Roles = result.UserInfo.Roles,
                    EnterpriseId = result.UserInfo.EnterpriseId,
                    Phone = result.UserInfo.Phone
                },
                Roles = result.Roles,
                ExpiresAt = result.ExpiresAt,
                Provider = result.Provider.ToString(),
                OnePortalToken = result.OnePortalToken
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("sso")]
    public async Task<ActionResult<AuthResponseDto>> SsoLogin([FromBody] SsoLoginRequestDto request)
    {
        try
        {
            var result = await _authenticationService.AuthenticateWithSsoAsync(request.SsoToken);
            
            var response = new AuthResponseDto
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserInfo = new UserInfoDto
                {
                    UserId = result.UserInfo.UserId,
                    Email = result.UserInfo.Email,
                    FirstName = result.UserInfo.FirstName,
                    LastName = result.UserInfo.LastName,
                    Roles = result.UserInfo.Roles,
                    EnterpriseId = result.UserInfo.EnterpriseId,
                    Phone = result.UserInfo.Phone
                },
                Roles = result.Roles,
                ExpiresAt = result.ExpiresAt,
                Provider = result.Provider.ToString(),
                OnePortalToken = result.OnePortalToken
            };

            return Ok(response);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { message = "SSO authentication is not yet implemented" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("has-users")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> HasUsers()
    {
        var anyUsers = await _userManager.Users.AnyAsync();
        return Ok(anyUsers);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var anyUsers = await _userManager.Users.AnyAsync();
            if (anyUsers)
            {
                return Forbid();
            }
            
            var user = await _userService.CreateUserAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone,
                role: "SUPERADMIN",
                enterpriseId: null,
                createdBy: null);

            var response = new UserResponseDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = "SUPERADMIN"
            };

            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var result = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
            
            var response = new AuthResponseDto
            {
                Token = result.Token,
                RefreshToken = result.RefreshToken,
                UserInfo = new UserInfoDto
                {
                    UserId = result.UserInfo.UserId,
                    Email = result.UserInfo.Email,
                    FirstName = result.UserInfo.FirstName,
                    LastName = result.UserInfo.LastName,
                    Roles = result.UserInfo.Roles,
                    EnterpriseId = result.UserInfo.EnterpriseId,
                    Phone = result.UserInfo.Phone
                },
                Roles = result.Roles,
                ExpiresAt = result.ExpiresAt,
                Provider = result.Provider.ToString(),
                OnePortalToken = result.OnePortalToken
            };

            return Ok(response);
        }
        catch (NotImplementedException)
        {
            return StatusCode(501, new { message = "Refresh token functionality is not yet fully implemented" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> GetCurrentUser()
    {
        try
        {
            // Extract user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var userInfo = await _authenticationService.GetUserInfoAsync(token);
            
            var response = new UserInfoDto
            {
                UserId = userInfo.UserId,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                Roles = userInfo.Roles,
                EnterpriseId = userInfo.EnterpriseId,
                Phone = userInfo.Phone
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}


using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VMS.Core.Entities;
using VMS.Core.Services.Authentication;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Services;

namespace VMS.Infrastructure.Services.Authentication;

public class IdentityAuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly VmsDbContext _context;

    public IdentityAuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        VmsDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _context = context;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    throw new UnauthorizedAccessException("Account is locked out");
                }
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenService.GenerateToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            var userInfo = new UserInfo
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                EnterpriseId = user.EnterpriseId,
                Phone = user.PhoneNumber
            };

            return new AuthenticationResult
            {
                Token = token,
                RefreshToken = refreshToken,
                UserInfo = userInfo,
                Roles = roles.ToList(),
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                Provider = AuthenticationProvider.Identity
            };
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    public async Task<AuthenticationResult> AuthenticateWithSsoAsync(string ssoToken)
    {
        // Future implementation for OnePortal SSO
        throw new NotImplementedException("SSO authentication will be implemented in a future phase");
    }

    public async Task<UserInfo> GetUserInfoAsync(string token)
    {
        var userId = _jwtTokenService.GetUserIdFromToken(token);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserInfo
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList(),
            EnterpriseId = user.EnterpriseId,
            Phone = user.PhoneNumber
        };
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var principal = _jwtTokenService.ValidateToken(token);
        return Task.FromResult(principal != null);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        // For now, we'll implement a simple refresh token validation
        // In production, you should store refresh tokens in the database
        // and validate them properly
        
        // This is a placeholder - in a real implementation, you would:
        // 1. Validate the refresh token against stored tokens
        // 2. Get the user ID from the refresh token
        // 3. Generate a new access token
        // 4. Optionally generate a new refresh token
        
        throw new NotImplementedException("Refresh token implementation will be enhanced in future");
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}


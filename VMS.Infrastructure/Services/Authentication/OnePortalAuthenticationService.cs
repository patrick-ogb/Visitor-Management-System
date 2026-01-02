using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using VMS.Core.Entities;
using VMS.Core.Services.Authentication;
using VMS.Infrastructure.Services;

namespace VMS.Infrastructure.Services.Authentication;

public class OnePortalAuthenticationService : IAuthenticationService
{
    private readonly IOnePortalApiService _onePortalApiService;
    private readonly IAuthenticationService _identityAuthenticationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<OnePortalAuthenticationService> _logger;

    public OnePortalAuthenticationService(
        IOnePortalApiService onePortalApiService,
        IAuthenticationService identityAuthenticationService,
        IJwtTokenService jwtTokenService,
        ILogger<OnePortalAuthenticationService> logger)
    {
        _onePortalApiService = onePortalApiService;
        _identityAuthenticationService = identityAuthenticationService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string email, string password)
    {
        // Try OnePortal authentication first
        try
        {
            _logger.LogInformation("Attempting OnePortal authentication for user: {Email}", email);
            var onePortalResponse = await _onePortalApiService.LoginAsync(email, password);

            if (onePortalResponse.Ok && onePortalResponse.Data != null)
            {
                _logger.LogInformation("OnePortal authentication successful for user: {Email}", email);
                return await MapOnePortalResponseToAuthenticationResult(onePortalResponse.Data, email);
            }
            else
            {
                _logger.LogWarning("OnePortal authentication failed for user: {Email}. Error: {Error}", 
                    email, onePortalResponse.Error);
                // OnePortal failed, fall through to Identity authentication
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OnePortal authentication failed for user: {Email}. Error: {Error}. Falling back to Identity authentication.", 
                email, ex.Message);
            // OnePortal failed, fall through to Identity authentication
        }

        // Fallback to Identity authentication if OnePortal failed
        try
        {
            _logger.LogInformation("Attempting Identity authentication for user: {Email}", email);
            var identityResult = await _identityAuthenticationService.AuthenticateAsync(email, password);
            _logger.LogInformation("Identity authentication successful for user: {Email}", email);
            return identityResult;
        }
        catch (Exception identityEx)
        {
            _logger.LogError(identityEx, "Both OnePortal and Identity authentication failed for user: {Email}", email);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }
    }

    public async Task<AuthenticationResult> AuthenticateWithSsoAsync(string ssoToken)
    {
        // Future implementation for OnePortal SSO
        throw new NotImplementedException("SSO authentication will be implemented in a future phase");
    }

    public async Task<UserInfo> GetUserInfoAsync(string token)
    {
        // For OnePortal tokens, we need to decode and extract user info
        // For now, delegate to Identity service which handles JWT validation
        return await _identityAuthenticationService.GetUserInfoAsync(token);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        // Validate our own JWT tokens
        return _identityAuthenticationService.ValidateTokenAsync(token);
    }

    public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
    {
        // Refresh token functionality - delegate to Identity service for now
        return await _identityAuthenticationService.RefreshTokenAsync(refreshToken);
    }

    private async Task<AuthenticationResult> MapOnePortalResponseToAuthenticationResult(
        VMS.Core.DTOs.OnePortalAuthDataDto onePortalData, 
        string email)
    {
        // Decode OnePortal JWT token to extract user information
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken? onePortalToken = null;
        
        try
        {
            onePortalToken = handler.ReadJwtToken(onePortalData.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode OnePortal JWT token");
        }

        // Extract email from token claims or use provided email
        var extractedEmail = onePortalToken?.Claims
            .FirstOrDefault(c => c.Type == "email" || c.Type == JwtRegisteredClaimNames.Email)?.Value 
            ?? email;

        // Extract name from token claims
        var nameClaim = onePortalToken?.Claims
            .FirstOrDefault(c => c.Type == "name" || c.Type == JwtRegisteredClaimNames.Name);
        
        var firstName = string.Empty;
        var lastName = string.Empty;
        
        if (nameClaim != null)
        {
            var nameParts = nameClaim.Value.Split(' ', 2);
            firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        }

        // Create a temporary ApplicationUser object for JWT generation
        // We use a temporary ID since we don't have a local user record
        var tempUser = new ApplicationUser
        {
            Id = onePortalData.Tenant.Id, // Use tenant ID as temporary user ID
            Email = extractedEmail,
            UserName = extractedEmail,
            FirstName = firstName,
            LastName = lastName,
            EnterpriseId = onePortalData.Tenant.Id,
            IsActive = onePortalData.Tenant.IsActive
        };

        // Generate our own JWT token using the OnePortal user data
        var roles = onePortalData.Roles ?? new List<string>();
        var token = _jwtTokenService.GenerateToken(tempUser, roles, onePortalData.AccessToken);

        // Create UserInfo from OnePortal response
        var userInfo = new UserInfo
        {
            UserId = tempUser.Id,
            Email = extractedEmail,
            FirstName = firstName,
            LastName = lastName,
            Roles = roles,
            EnterpriseId = onePortalData.Tenant.Id,
            Phone = null // Phone not available in OnePortal response
        };

        // Calculate expiration time from expiresIn (in seconds)
        var expiresAt = DateTime.UtcNow.AddSeconds(onePortalData.ExpiresIn);

        return new AuthenticationResult
        {
            Token = token,
            RefreshToken = onePortalData.RefreshToken,
            UserInfo = userInfo,
            Roles = roles,
            ExpiresAt = expiresAt,
            Provider = AuthenticationProvider.OnePortalSso,
            OnePortalToken = onePortalData.AccessToken
        };
    }
}


using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VMS.Core.Entities;
using VMS.Core.Services.Authentication;

namespace VMS.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationInMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secret = configuration["JwtSettings:Secret"] 
            ?? throw new InvalidOperationException("JWT Secret is not configured");
        _issuer = configuration["JwtSettings:Issuer"] ?? "VMS.API";
        _audience = configuration["JwtSettings:Audience"] ?? "VMS.Client";
        _expirationInMinutes = int.Parse(configuration["JwtSettings:ExpirationInMinutes"] ?? "60");
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles, string? onePortalToken = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secret);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("name", $"{user.FirstName} {user.LastName}"),
        };

        // Add roles as claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add enterprise ID if present
        if (user.EnterpriseId.HasValue)
        {
            claims.Add(new Claim("enterpriseId", user.EnterpriseId.Value.ToString()));
        }

        // Add authentication provider
        claims.Add(new Claim("authProvider", AuthenticationProvider.Identity.ToString()));

        // Add OnePortal token if provided
        if (!string.IsNullOrWhiteSpace(onePortalToken))
        {
            claims.Add(new Claim("onePortalToken", onePortalToken));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationInMinutes),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public int GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null)
            throw new UnauthorizedAccessException("Invalid token");

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID in token");

        return userId;
    }

    public List<string> GetRolesFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null)
            return new List<string>();

        return principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
    }
}





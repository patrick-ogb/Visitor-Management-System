using System.Security.Claims;
using VMS.Core.Entities;

namespace VMS.Infrastructure.Services;

public interface IJwtTokenService
{
    string GenerateToken(ApplicationUser user, IList<string> roles, string? onePortalToken = null);
    ClaimsPrincipal? ValidateToken(string token);
    int GetUserIdFromToken(string token);
    List<string> GetRolesFromToken(string token);
}





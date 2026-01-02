using VMS.Core.Entities;

namespace VMS.Infrastructure.Services;

public interface IUserService
{
    Task<ApplicationUser> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string? phone,
        string role,
        int? enterpriseId,
        int? createdBy);
    
    Task<bool> AssignRoleAsync(ApplicationUser user, string role);
}





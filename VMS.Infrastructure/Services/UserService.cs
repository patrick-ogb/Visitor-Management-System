using Microsoft.AspNetCore.Identity;
using VMS.Core.Entities;

namespace VMS.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUser> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string? phone,
        string role,
        int? enterpriseId,
        int? createdBy)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            EnterpriseId = enterpriseId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Assign role
        if (!string.IsNullOrWhiteSpace(role))
        {
            await AssignRoleAsync(user, role);
        }

        return user;
    }

    public async Task<bool> AssignRoleAsync(ApplicationUser user, string role)
    {
        var result = await _userManager.AddToRoleAsync(user, role);
        return result.Succeeded;
    }
}





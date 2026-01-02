using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VMS.Core.Entities;
using VMS.Infrastructure.Data;

namespace VMS.Infrastructure.Data.Seeding;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Define roles
        var roles = new[] { "SUPERADMIN", "LFZAdmin", "LFZStaff", "EnterpriseAdmin", "EnterpriseUser", "GateAdmin" };
        
        // Update existing "Superadmin" role to "SUPERADMIN" if it exists
        var oldRole = await roleManager.FindByNameAsync("Superadmin");
        if (oldRole != null)
        {
            oldRole.Name = "SUPERADMIN";
            oldRole.NormalizedName = "SUPERADMIN";
            await roleManager.UpdateAsync(oldRole);
        }

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }

        // Note: intentionally skipping default admin user creation.
        // First user bootstrap must happen via the guarded registration flow.
    }
}


using MediatR;
using Microsoft.AspNetCore.Identity;
using VMS.API.Common.Models;
using VMS.Core.Entities;

namespace VMS.API.Commands.Users;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand, BaseResponse<bool>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager;

    public UpdateUserRoleCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<BaseResponse<bool>> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find the user
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                return BaseResponse<bool>.ErrorResponse("User not found");
            }

            // Validate role exists
            if (!await _roleManager.RoleExistsAsync(request.NewRole))
            {
                return BaseResponse<bool>.ErrorResponse($"Role '{request.NewRole}' does not exist");
            }

            // Validate EnterpriseId for Enterprise roles
            var enterpriseRoles = new[] { "EnterpriseUser", "EnterpriseAdmin" };
            if (enterpriseRoles.Contains(request.NewRole) && !request.EnterpriseId.HasValue)
            {
                return BaseResponse<bool>.ErrorResponse($"EnterpriseId is required for role '{request.NewRole}'");
            }

            // Remove user from all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return BaseResponse<bool>.ErrorResponse($"Failed to remove existing roles: {errors}");
                }
            }

            // Update EnterpriseId if provided
            if (request.EnterpriseId.HasValue)
            {
                user.EnterpriseId = request.EnterpriseId.Value;
            }
            else if (enterpriseRoles.Contains(request.NewRole))
            {
                // EnterpriseId should already be set from the request validation above
                // This is just a safety check
            }
            else
            {
                // For non-enterprise roles, clear EnterpriseId
                user.EnterpriseId = null;
            }

            await _userManager.UpdateAsync(user);

            // Add user to new role
            var addResult = await _userManager.AddToRoleAsync(user, request.NewRole);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return BaseResponse<bool>.ErrorResponse($"Failed to assign role: {errors}");
            }

            return BaseResponse<bool>.SuccessResponse(true, "User role updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<bool>.ErrorResponse($"An error occurred: {ex.Message}");
        }
    }
}


using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Core.Entities;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.Users;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, BaseResponse<PagedResponse<UserDto>>>
{
    private readonly VmsDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetUsersQueryHandler(VmsDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<BaseResponse<PagedResponse<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        // Filter by enterprise
        if (request.EnterpriseId.HasValue)
        {
            query = query.Where(u => u.EnterpriseId == request.EnterpriseId.Value);
        }

        // Filter by search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm) ||
                (u.Email != null && u.Email.ToLower().Contains(searchTerm)));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var users = await query
            .Include(u => u.Enterprise)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userDtos.Add(new UserDto
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                Roles = roles.ToList(),
                EnterpriseId = user.EnterpriseId,
                EnterpriseName = user.Enterprise?.Name,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }

        // Filter by role if specified
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            userDtos = userDtos.Where(u => u.Roles.Contains(request.Role)).ToList();
            totalCount = userDtos.Count;
        }

        var response = new PagedResponse<UserDto>
        {
            Items = userDtos,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };

        return BaseResponse<PagedResponse<UserDto>>.SuccessResponse(response);
    }
}



















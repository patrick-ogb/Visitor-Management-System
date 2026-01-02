using MediatR;
using Microsoft.EntityFrameworkCore;
using VMS.API.Common.Models;
using VMS.Infrastructure.Data;

namespace VMS.API.Queries.EnterpriseUsers;

public class GetEnterpriseUsersByEnterpriseQueryHandler : IRequestHandler<GetEnterpriseUsersByEnterpriseQuery, BaseResponse<List<Queries.Users.UserDto>>>
{
    private readonly VmsDbContext _context;

    public GetEnterpriseUsersByEnterpriseQueryHandler(VmsDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<List<Queries.Users.UserDto>>> Handle(GetEnterpriseUsersByEnterpriseQuery request, CancellationToken cancellationToken)
    {
        var query = _context.EnterpriseUsers.AsQueryable();

        // Filter by EnterpriseId (local) or OnePortalEnterpriseId
        if (request.EnterpriseId.HasValue)
        {
            query = query.Where(eu => eu.EnterpriseId == request.EnterpriseId.Value);
        }
        else if (request.OnePortalEnterpriseId.HasValue)
        {
            query = query.Where(eu => eu.OnePortalEnterpriseId == request.OnePortalEnterpriseId.Value);
        }
        else
        {
            // If neither filter is provided, return empty list
            return BaseResponse<List<Queries.Users.UserDto>>.SuccessResponse(new List<Queries.Users.UserDto>());
        }

        var enterpriseUsers = await query
            .Include(eu => eu.Enterprise)
            .OrderBy(eu => eu.FullName)
            .ToListAsync(cancellationToken);

        var userDtos = enterpriseUsers.Select(eu =>
        {
            // Parse FullName into FirstName and LastName
            var nameParts = eu.FullName.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameParts.Length > 0 ? nameParts[0] : string.Empty;
            var lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            // Convert RoleName to Roles list
            var roles = new List<string>();
            if (!string.IsNullOrWhiteSpace(eu.RoleName))
            {
                roles.Add(eu.RoleName);
            }

            return new Queries.Users.UserDto
            {
                UserId = eu.EnterpriseUserId, // Use EnterpriseUserId (primary key) as UserId for frontend dropdown
                Email = eu.EmailAddress,
                FirstName = firstName,
                LastName = lastName,
                Phone = null, // Not stored in EnterpriseUsers
                Roles = roles,
                EnterpriseId = eu.EnterpriseId,
                EnterpriseName = eu.Enterprise?.Name,
                IsActive = eu.IsActive,
                CreatedAt = eu.CreatedAt
            };
        }).ToList();

        return BaseResponse<List<Queries.Users.UserDto>>.SuccessResponse(userDtos);
    }
}


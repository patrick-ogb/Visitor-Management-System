using MediatR;
using VMS.API.Common.Models;

namespace VMS.API.Queries.Users;

public class GetUsersQuery : IRequest<BaseResponse<PagedResponse<UserDto>>>
{
    public int? EnterpriseId { get; set; }
    public string? Role { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = new();
    public int? EnterpriseId { get; set; }
    public string? EnterpriseName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}





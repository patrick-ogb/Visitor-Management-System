namespace VMS.Client.DTOs.Users;

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


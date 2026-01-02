namespace VMS.Client.DTOs.Auth;

public class UserInfoDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public int? EnterpriseId { get; set; }
    public string? Phone { get; set; }
}





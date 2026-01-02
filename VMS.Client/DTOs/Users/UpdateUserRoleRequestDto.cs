namespace VMS.Client.DTOs.Users;

public class UpdateUserRoleRequestDto
{
    public int UserId { get; set; }
    public string NewRole { get; set; } = string.Empty;
    public int? EnterpriseId { get; set; }
}


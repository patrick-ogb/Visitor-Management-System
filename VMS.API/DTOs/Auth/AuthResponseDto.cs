namespace VMS.API.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfoDto UserInfo { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? OnePortalToken { get; set; }
}








namespace VMS.Core.Services.Authentication;

public class AuthenticationResult
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo UserInfo { get; set; } = new();
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
    public AuthenticationProvider Provider { get; set; }
    public string? OnePortalToken { get; set; }
}








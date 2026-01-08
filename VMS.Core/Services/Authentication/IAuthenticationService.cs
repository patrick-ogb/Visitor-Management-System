namespace VMS.Core.Services.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResult> AuthenticateAsync(string email, string password);
    Task<AuthenticationResult> AuthenticateWithSsoAsync(string ssoToken);
    Task<UserInfo> GetUserInfoAsync(string token);
    Task<bool> ValidateTokenAsync(string token);
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
}




















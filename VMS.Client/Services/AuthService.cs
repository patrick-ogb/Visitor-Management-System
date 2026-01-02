using System.Net.Http.Json;
using Blazored.LocalStorage;
using VMS.Client.DTOs.Auth;

namespace VMS.Client.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthenticationStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(
        HttpClient httpClient,
        CustomAuthenticationStateProvider authStateProvider,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResponseDto?> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequestDto { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                if (authResponse != null)
                {
                    var userInfoJson = System.Text.Json.JsonSerializer.Serialize(authResponse.UserInfo);
                    await _authStateProvider.MarkUserAsAuthenticated(
                        authResponse.Token,
                        authResponse.RefreshToken,
                        userInfoJson,
                        authResponse.OnePortalToken);
                }
                return authResponse;
            }
            
            return null;
        }
        catch(Exception ex)
        {
            var err = ex.Message;
            return null;
        }
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.MarkUserAsLoggedOut();
    }

    public async Task<UserInfoDto?> GetCurrentUserAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/me");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}


using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace VMS.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        var user = new ClaimsPrincipal(identity);
        
        return new AuthenticationState(user);
    }

    public async Task MarkUserAsAuthenticated(string token, string refreshToken, string userInfoJson, string? onePortalToken = null)
    {
        await _localStorage.SetItemAsStringAsync("authToken", token);
        await _localStorage.SetItemAsStringAsync("refreshToken", refreshToken);
        await _localStorage.SetItemAsStringAsync("userInfo", userInfoJson);
        
        if (!string.IsNullOrWhiteSpace(onePortalToken))
        {
            await _localStorage.SetItemAsStringAsync("onePortalToken", onePortalToken);
        }
        
        var identity = new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt");
        var user = new ClaimsPrincipal(identity);
        
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("userInfo");
        await _localStorage.RemoveItemAsync("onePortalToken");
        
        var identity = new ClaimsIdentity();
        var user = new ClaimsPrincipal(identity);
        
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task<string?> GetOnePortalTokenAsync()
    {
        return await _localStorage.GetItemAsStringAsync("onePortalToken");
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<Claim>();
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                var element = kvp.Value;
                var claimType = kvp.Key;
                
                // Map JWT claim types to .NET claim types for proper authorization
                if (claimType == "role" || claimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                {
                    claimType = ClaimTypes.Role;
                }
                else if (claimType == "sub" || claimType == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                {
                    claimType = ClaimTypes.NameIdentifier;
                }
                else if (claimType == "email" || claimType == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                {
                    claimType = ClaimTypes.Email;
                }
                else if (claimType == "name" || claimType == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")
                {
                    claimType = ClaimTypes.Name;
                }
                
                if (element.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in element.EnumerateArray())
                    {
                        claims.Add(new Claim(claimType, GetClaimValue(item)));
                    }
                }
                else
                {
                    claims.Add(new Claim(claimType, GetClaimValue(element)));
                }
            }
        }

        return claims;
    }

    private string GetClaimValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}


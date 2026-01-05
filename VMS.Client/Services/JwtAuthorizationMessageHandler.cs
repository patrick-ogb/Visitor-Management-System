using Blazored.LocalStorage;
using System.Net.Http.Headers;

namespace VMS.Client.Services;

public class JwtAuthorizationMessageHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;

    public JwtAuthorizationMessageHandler(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _localStorage.GetItemAsStringAsync("authToken");
        
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 - Unauthorized
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Optionally try to refresh token here
            // For now, we'll just let it fail and the app can handle it
        }

        return response;
    }
}



















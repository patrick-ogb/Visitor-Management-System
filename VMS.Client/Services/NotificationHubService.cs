using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using VMS.Client.DTOs.Notifications;

namespace VMS.Client.Services;

public class NotificationHubService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<NotificationHubService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private Timer? _heartbeatTimer;

    public event Action<NotificationDto>? OnNotificationReceived;
    public event Action<int>? OnUnreadCountChanged;

    public NotificationHubService(
        ILocalStorageService localStorage,
        ILogger<NotificationHubService> logger,
        IConfiguration configuration)
    {
        _localStorage = localStorage;
        _logger = logger;

        // Get base address from configuration
        var apiBaseAddress = configuration["ApiBaseAddress"];
        if (string.IsNullOrWhiteSpace(apiBaseAddress))
        {
            apiBaseAddress = "https://localhost:7228/";
        }
        if (!apiBaseAddress.EndsWith("/"))
        {
            apiBaseAddress += "/";
        }

        // Create HttpClient with JWT interceptor for this singleton service
        var handler = new JwtAuthorizationMessageHandler(localStorage)
        {
            InnerHandler = new HttpClientHandler()
        };
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiBaseAddress)
        };

        var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? "https://localhost:7228";
        var hubUrl = $"{baseUrl}/notificationhub";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Get token from localStorage
                options.AccessTokenProvider = async () =>
                {
                    var token = await GetTokenFromStorage();
                    return token;
                };
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();

        _hubConnection.On<NotificationDto>("ReceiveNotification", (notification) =>
        {
            OnNotificationReceived?.Invoke(notification);
            UpdateUnreadCount();
        });

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR connection lost. Reconnecting...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected. Connection ID: {ConnectionId}", connectionId);
            StartHeartbeat();
            UpdateUnreadCount();
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogError(error, "SignalR connection closed");
            StopHeartbeat();
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connection started");
                StartHeartbeat();
                await UpdateUnreadCountAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting SignalR connection");
        }
    }

    public async Task StopAsync()
    {
        StopHeartbeat();
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
            _logger.LogInformation("SignalR connection stopped");
        }
    }

    private void StartHeartbeat()
    {
        _heartbeatTimer = new Timer(async _ =>
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("SendHeartbeat");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat");
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private void StopHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    private async Task<string?> GetTokenFromStorage()
    {
        try
        {
            var token = await _localStorage.GetItemAsStringAsync("authToken");
            return token;
        }
        catch
        {
            return null;
        }
    }

    private void UpdateUnreadCount()
    {
        _ = UpdateUnreadCountAsync();
    }

    private async Task UpdateUnreadCountAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/Notifications/unread?limit=1");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<VMS.Client.DTOs.Common.BaseResponse<List<NotificationDto>>>();
                if (result?.Data != null)
                {
                    OnUnreadCountChanged?.Invoke(result.Data.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unread count");
        }
    }

    public async ValueTask DisposeAsync()
    {
        StopHeartbeat();
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
        _httpClient?.Dispose();
    }
}


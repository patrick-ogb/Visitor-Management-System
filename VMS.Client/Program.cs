using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using VMS.Client;
using VMS.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure base address for API
var apiBaseAddress = builder.Configuration["ApiBaseAddress"];
if (string.IsNullOrWhiteSpace(apiBaseAddress))
{
    // Fallback to API default port if not configured
    apiBaseAddress = "https://localhost:7228/";
}
if (!apiBaseAddress.EndsWith("/"))
{
    apiBaseAddress += "/";
}

// Register LocalStorage as singleton (safe in Blazor WebAssembly - localStorage is shared per browser tab)
builder.Services.AddBlazoredLocalStorageAsSingleton();

// Register Authentication State Provider
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthenticationStateProvider>());

// Register JWT Authorization Message Handler
builder.Services.AddScoped<JwtAuthorizationMessageHandler>();

// Configure HttpClient with JWT interceptor
builder.Services.AddScoped<HttpClient>(sp =>
{
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    var handler = new JwtAuthorizationMessageHandler(localStorage)
    {
        InnerHandler = new HttpClientHandler()
    };
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };
});

// Register Auth Service
builder.Services.AddScoped<AuthService>();

// Register Toast Service
builder.Services.AddScoped<IToastService, ToastService>();

// Register Notification Hub Service (singleton to maintain connection)
builder.Services.AddSingleton<NotificationHubService>();

// Add Authorization with matching server policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("RequireLFZAdmin", policy => policy.RequireRole("SUPERADMIN", "LFZAdmin"));
    options.AddPolicy("RequireLFZStaff", policy => policy.RequireRole("SUPERADMIN", "LFZStaff"));
    options.AddPolicy("RequireEnterpriseAdmin", policy => policy.RequireRole("SUPERADMIN", "EnterpriseAdmin"));
    options.AddPolicy("RequireEnterpriseUser", policy => policy.RequireRole("SUPERADMIN", "EnterpriseUser"));
    options.AddPolicy("RequireGateAdmin", policy => policy.RequireRole("SUPERADMIN", "GateAdmin"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("SUPERADMIN", "LFZAdmin", "EnterpriseAdmin"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("SUPERADMIN", "LFZStaff", "EnterpriseAdmin", "EnterpriseUser"));
});

await builder.Build().RunAsync();

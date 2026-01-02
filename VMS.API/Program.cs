using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VMS.Core.Entities;
using VMS.Infrastructure.Hubs;
using VMS.Core.Services.Authentication;
using VMS.Infrastructure.Data;
using VMS.Infrastructure.Data.Seeding;
using VMS.Infrastructure.Repositories;
using VMS.Infrastructure.Services;
using VMS.Infrastructure.Services.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "VMS API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<VmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<VmsDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] 
    ?? throw new InvalidOperationException("JWT Secret is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "VMS.API",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "VMS.Client",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Configure JWT for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationhub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireLFZAdmin", policy => policy.RequireRole("SUPERADMIN", "LFZAdmin"));
    options.AddPolicy("RequireLFZStaff", policy => policy.RequireRole("SUPERADMIN", "LFZStaff"));
    options.AddPolicy("RequireEnterpriseAdmin", policy => policy.RequireRole("SUPERADMIN", "EnterpriseAdmin"));
    options.AddPolicy("RequireEnterpriseUser", policy => policy.RequireRole("SUPERADMIN", "EnterpriseUser"));
    options.AddPolicy("RequireGateAdmin", policy => policy.RequireRole("SUPERADMIN", "GateAdmin"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("SUPERADMIN", "LFZAdmin", "EnterpriseAdmin"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("SUPERADMIN", "LFZStaff", "EnterpriseAdmin", "EnterpriseUser"));
});

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Register SignalR
builder.Services.AddSignalR();

// Register Notification Services
builder.Services.AddSingleton<NotificationEventQueue>();
builder.Services.AddScoped<INotificationEventService, NotificationEventService>();
builder.Services.AddScoped<IPresenceService, PresenceService>();
builder.Services.AddScoped<INotificationProcessor>(sp =>
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var presenceService = sp.GetRequiredService<IPresenceService>();
    var emailService = sp.GetRequiredService<IEmailService>();
    var hubContext = sp.GetRequiredService<IHubContext<NotificationHub>>();
    var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<NotificationProcessor>>();
    var context = sp.GetRequiredService<VmsDbContext>();
    return new NotificationProcessor(unitOfWork, presenceService, emailService, hubContext, userManager, configuration, logger, context);
});

// Register Notification Background Service
builder.Services.AddHostedService<NotificationBackgroundService>();

// Register Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IInvitationNumberService, InvitationNumberService>();

// Register Unit of Work and Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register HttpClient for OnePortal API (this also registers IOnePortalApiService)
builder.Services.AddHttpClient<IOnePortalApiService, OnePortalApiService>();

// Register Identity Authentication Service (for fallback)
builder.Services.AddScoped<IdentityAuthenticationService>();

// Register Authentication Service (pluggable)
// OnePortalAuthenticationService will try OnePortal first, then fallback to Identity
var authProvider = builder.Configuration["Authentication:Provider"] ?? "OnePortal";
if (authProvider == "Identity")
{
    builder.Services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
}
else if (authProvider == "OnePortal" || authProvider == "OnePortalSso")
{
    // Register OnePortalAuthenticationService which handles fallback to Identity internally
    builder.Services.AddScoped<IAuthenticationService>(sp =>
    {
        var onePortalApiService = sp.GetRequiredService<IOnePortalApiService>();
        var identityAuthService = sp.GetRequiredService<IdentityAuthenticationService>();
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();
        var logger = sp.GetRequiredService<ILogger<OnePortalAuthenticationService>>();
        return new OnePortalAuthenticationService(onePortalApiService, identityAuthService, jwtTokenService, logger);
    });
}
else
{
    builder.Services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? new[] { 
            "https://localhost:7175", 
            "http://localhost:5160",
            "https://localhost:7000", 
            "http://localhost:5000" 
        })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed roles and initial admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await RoleSeeder.SeedRolesAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notificationhub");

app.Run();

using LibrarySystem.Business.BackgroundJobs;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Business.Messaging;
using LibrarySystem.Business.Notifications;
using LibrarySystem.Business.Services;
using LibrarySystem.DataAccess.Context;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Authentication;
using LibrarySystem.Shared.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RabbitMQ.Client;
using System.Text;

namespace LibrarySystem.API.Extensions;

/// <summary>
/// Composition root: registers all application services with appropriate
/// lifetimes and configures authentication, SignalR and Swagger.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers business/data-access services, JWT authentication, SignalR,
    /// hosted background services and Swagger.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddLibrarySystemServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Data access
        services.AddDbContext<LibraryDBContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Settings
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection(RabbitMqSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<BackgroundJobSettings>()
            .Bind(configuration.GetSection(BackgroundJobSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection(SmtpSettings.SectionName))
            .ValidateOnStart();
        services.AddOptions<AppSettings>()
            .Bind(configuration.GetSection(AppSettings.SectionName))
            .ValidateOnStart();

        // CORS for the browser clients (Angular dev server, gateway origins).
        // Required for SignalR negotiate/websocket requests that carry Origin.
        var appSettings = configuration.GetSection(AppSettings.SectionName).Get<AppSettings>()
                          ?? new AppSettings();
        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(appSettings.CorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        // Email delivery (best-effort; logs content when SMTP is unconfigured).
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Authentication (JWT bearer). Secrets come from configuration/user secrets only.
        var jwt = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                // Allow SignalR clients to pass the token in the access_token query
                // string during WebSocket negotiation.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        services.AddAuthorization();
        services.AddSingleton<IJwtService, JwtService>();

        // Business services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBorrowingService, BorrowingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IUserService, UserService>();

        // Messaging infrastructure (singleton connection shared by publisher/consumer)
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IBorrowRequestPublisher, BorrowRequestPublisher>();
        services.AddHostedService<BorrowRequestConsumer>();

        // Real-time notifications + expiration job
        services.AddSignalR();
        services.AddSingleton<INotificationDispatcher, SignalRNotificationDispatcher>();
        services.AddHostedService<BorrowingExpirationJob>();

        // API surface
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGenWithAuth();

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI including the Bearer security scheme so the
    /// frontend can authenticate directly from the UI.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configured service collection.</returns>
    private static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Library System API",
                Version = "1.0.0",
                Description = "Backend API for the Library System: book catalog, borrowing requests, " +
                              "admin review and notifications."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token obtained from POST /api/auth/login."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document),
                    []
                }
            });
        });

        return services;
    }
}

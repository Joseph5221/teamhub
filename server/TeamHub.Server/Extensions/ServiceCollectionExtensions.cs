using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeamHub.Server.Modules.Auth;
using TeamHub.Server.Modules.Dashboard;
using TeamHub.Server.Modules.Integrations;
using TeamHub.Server.Modules.Integrations.GitHub;
using TeamHub.Server.Modules.Projects;
using TeamHub.Server.Modules.Teams;
using TeamHub.Server.Modules.Users;
using TeamHub.Server.Infrastructure.Data;
using TeamHub.Server.Infrastructure.Security;

namespace TeamHub.Server.Extensions;

/// <summary>
/// Extension methods for configuring services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds infrastructure services (database, authentication, etc.)
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add database
        services.AddDbContext<AppDbContext>(options =>
        {
            // Use in-memory database for development
            // Switch to SQL Server, PostgreSQL, etc. for production
            options.UseInMemoryDatabase("TeamHubDb");
            
            // For SQLite (uncomment if preferred):
            // options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
        });

        // Configure JWT options
        var jwtSection = configuration.GetSection("Jwt");
        services.Configure<JwtOptions>(jwtSection);

        var jwtSecret = configuration["Jwt:Secret"];
        var jwtIssuer = configuration["Jwt:Issuer"];
        var jwtAudience = configuration["Jwt:Audience"];

        // Validate that secrets are configured
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException(
                "JWT Secret is not configured. " +
                "Please set it using user secrets (dotnet user-secrets set \"Jwt:Secret\" \"your-secret\") " +
                "or environment variables.");
        }

        if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
        {
            throw new InvalidOperationException(
                "JWT Issuer and Audience must be configured.");
        }

        // JWT Authentication setup
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        // Register infrastructure services
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IPasswordValidator, PasswordValidator>();

        return services;
    }

    /// <summary>
    /// Adds feature services
    /// </summary>
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        // Register feature services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();

        // Integration connectors — each registers itself as an
        // IModuleConnector; IntegrationService picks the right one by
        // IntegrationType. Add one AddScoped<IModuleConnector, ...> line
        // per connector as new ones are built (Jira, Slack, Calendar).
        services.AddScoped<IModuleConnector, GitHubConnector>();

        // GitHub's HTTP client: retry + circuit-breaker per
        // docs/ARCHITECTURE.md "Resilience & Testing". Auth is a per-team
        // personal access token read from Integration.ConfigurationData at
        // call time (see GitHubConnector), not a header on this client.
        services.AddHttpClient<IGitHubApiClient, GitHubApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("TeamHub-Server");
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(GitHubResiliencePolicies.GetRetryPolicy())
        .AddPolicyHandler(GitHubResiliencePolicies.GetCircuitBreakerPolicy());

        return services;
    }

    /// <summary>
    /// Adds API documentation (Swagger)
    /// </summary>
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "TeamHub API",
                Version = "v1",
                Description = "API for TeamHub project management platform"
            });

            // Add JWT authentication to Swagger
            options.AddSecurityDefinition("Bearer", new()
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Enter your JWT token"
            });

            options.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
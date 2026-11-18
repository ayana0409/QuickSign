using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using QuickSign.Application;
using QuickSign.Domain;
using QuickSign.Domain.Shared;
using QuickSign.EntityFrameworkCore;
using QuickSign.HttpApi;
using QuickSign.HttpApi.Host.Middlewares;
using QuickSign.Infrastructure;

namespace QuickSign.HttpApi.Host.Configuration;

/// <summary>
/// Extension methods for registering host dependencies, configuring authentication, CORS, Swagger, and HTTP request pipeline.
/// </summary>
public static class HostConfigurationExtensions
{
    private const string DefaultCorsPolicyName = "DefaultCorsPolicy";

    /// <summary>
    /// Registers all application layers, API behavior, JWT authentication, CORS, and Swagger generator.
    /// </summary>
    public static IServiceCollection AddHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLayeredArchitectureServices(configuration);
        services.ConfigureApiBehavior();
        services.ConfigureJwtAuthentication(configuration);
        services.ConfigureCorsPolicy(configuration);
        services.ConfigureSwagger();

        return services;
    }

    /// <summary>
    /// Configures the complete HTTP request middleware pipeline.
    /// </summary>
    public static WebApplication UseHostPipeline(this WebApplication app)
    {
        // Global exception handling
        app.UseMiddleware<BusinessExceptionHandlingMiddleware>();

        // Swagger and API documentation
        app.UseSwaggerDocumentation();

        // Security, Routing, and Controllers
        app.UseCors(DefaultCorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Configures Swagger and Swagger UI endpoints including root redirect.
    /// </summary>
    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickSign Management API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "QuickSign API Documentation";
            c.DisplayRequestDuration();
        });

        // Redirect root URL to Swagger UI for convenient developer navigation
        app.MapGet("/", () => Results.Redirect("/swagger"));

        return app;
    }

    /// <summary>
    /// Registers dependencies from all architectural layers (Domain, Application, Infrastructure, EF Core, HttpApi).
    /// </summary>
    private static IServiceCollection AddLayeredArchitectureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddDomainServices();
        services.AddApplicationServices();
        services.AddEntityFrameworkCoreServices(configuration);
        services.AddInfrastructureServices(configuration);
        services.AddHttpApiControllers();

        return services;
    }

    /// <summary>
    /// Configures uniform 400 Bad Request responses for FluentValidation and model binding errors.
    /// </summary>
    private static IServiceCollection ConfigureApiBehavior(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => JsonNamingPolicy.CamelCase.ConvertName(kvp.Key),
                        kvp => (object)kvp.Value!.Errors.Select(err => err.ErrorMessage).ToArray()
                    );

                var payload = new Dictionary<string, object?>
                {
                    ["errorCode"] = QuickSignDomainErrorCodes.InvalidPayload,
                    ["httpStatus"] = StatusCodes.Status400BadRequest,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["traceId"] = traceId,
                    ["details"] = errors
                };

                return new BadRequestObjectResult(payload);
            };
        });

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication against the configured SSO Identity Provider.
    /// </summary>
    private static IServiceCollection ConfigureJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var identityAuthority = configuration["SsoIdentity:Authority"];
        if (string.IsNullOrWhiteSpace(identityAuthority))
        {
            throw new InvalidOperationException("Configuration 'SsoIdentity:Authority' is required and cannot be empty.");
        }

        var normalizedAuthority = identityAuthority.TrimEnd('/');

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = normalizedAuthority;
            options.RequireHttpsMetadata = false;
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    normalizedAuthority,
                    normalizedAuthority + "/"
                },
                ValidateAudience = false, // SSO JWT tokens do not enforce strict audience validation
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5),
                RoleClaimType = "role",
                NameClaimType = "preferred_username"
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (!string.IsNullOrWhiteSpace(authHeader))
                    {
                        var parts = authHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = parts[^1].Trim();
                        }
                        else if (parts.Length == 1 && !parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = parts[0].Trim();
                        }
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    // Suppress default challenge response to return unified JSON error payload
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    string failureMessage;

                    if (context.AuthenticateFailure != null)
                    {
                        failureMessage = context.AuthenticateFailure.Message;
                    }
                    else if (string.IsNullOrWhiteSpace(authHeader))
                    {
                        failureMessage = "No Authorization header was received by the server. Please ensure you clicked the 'Authorize' button in Swagger UI (the padlock icon must be closed/locked).";
                    }
                    else
                    {
                        failureMessage = context.ErrorDescription ?? context.Error ?? "User is not authenticated or token is invalid.";
                    }

                    var payload = new Dictionary<string, object?>
                    {
                        ["errorCode"] = QuickSignDomainErrorCodes.Unauthorized,
                        ["httpStatus"] = StatusCodes.Status401Unauthorized,
                        ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                        ["traceId"] = traceId,
                        ["message"] = failureMessage
                    };

                    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await context.Response.WriteAsync(json);
                },
                OnForbidden = async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                    var payload = new Dictionary<string, object?>
                    {
                        ["errorCode"] = QuickSignDomainErrorCodes.ForbiddenAccess,
                        ["httpStatus"] = StatusCodes.Status403Forbidden,
                        ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                        ["traceId"] = traceId
                    };

                    var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    await context.Response.WriteAsync(json);
                }
            };
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Configures Cross-Origin Resource Sharing (CORS) with Fail-Fast validation of allowed origins.
    /// </summary>
    private static IServiceCollection ConfigureCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins == null || allowedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Configuration 'Cors:AllowedOrigins' is required and cannot be empty.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultCorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configures Swagger generator with OpenAPI v1 specifications, Bearer JWT, and API Key authentication schemes.
    /// </summary>
    private static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "QuickSign Management Service API",
                Version = "v1",
                Description = "Core management, certificate lifecycle, and document verification API for QuickSign platform."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter your JWT Bearer token. You can paste the raw token (eyJ...) or Bearer {token}.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Description = "API Key authentication header for external client integrations. Format: qs_live_...",
                Name = "X-API-Key",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "ApiKey"
            });

            options.AddSecurityRequirement((doc) => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", doc),
                    new List<string>()
                }
            });

            // Include XML comments from compiled assemblies for rich Swagger UI documentation
            var xmlFiles = new[]
            {
                $"{typeof(HostConfigurationExtensions).Assembly.GetName().Name}.xml",
                "QuickSign.HttpApi.xml",
                "QuickSign.Application.Contracts.xml"
            };

            foreach (var xmlFile in xmlFiles)
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            }
        });

        return services;
    }
}

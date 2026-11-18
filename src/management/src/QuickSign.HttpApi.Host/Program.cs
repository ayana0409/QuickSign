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

var builder = WebApplication.CreateBuilder(args);

// 1. Register Layered Architecture Dependencies
builder.Services.AddHttpContextAccessor();
builder.Services.AddDomainServices();
builder.Services.AddApplicationServices();
builder.Services.AddEntityFrameworkCoreServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHttpApiControllers();

// Configure unified validation error responses for model binding / FluentValidation
builder.Services.Configure<ApiBehaviorOptions>(options =>
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

// 2. Configure JWT Bearer Authentication with SSO Identity Server
var identityAuthority = builder.Configuration["SsoIdentity:Authority"];
if (string.IsNullOrWhiteSpace(identityAuthority))
{
    throw new InvalidOperationException("Configuration 'SsoIdentity:Authority' is required and cannot be empty.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = identityAuthority;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = identityAuthority,
        ValidateAudience = false, // SSO JWT does not require strict audience validation
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            // Suppress default challenge logic to return unified JSON error payload
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            var payload = new Dictionary<string, object?>
            {
                ["errorCode"] = QuickSignDomainErrorCodes.Unauthorized,
                ["httpStatus"] = StatusCodes.Status401Unauthorized,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["traceId"] = traceId
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

builder.Services.AddAuthorization();

// 3. Configure CORS Policy
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Configuration 'Cors:AllowedOrigins' is required and cannot be empty.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 4. Configure Swagger with JWT Bearer Definition
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuickSign Management Service API",
        Version = "v1",
        Description = "Core management, certificate lifecycle, and document verification API for QuickSign platform."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement((doc) => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 5. Middleware Pipeline
app.UseMiddleware<BusinessExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuickSign Management API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

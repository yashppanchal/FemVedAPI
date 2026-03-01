using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace FemVed.API.Extensions;

/// <summary>
/// Registers API-layer services: Swagger, JWT authentication, authorization policies, and rate limiting.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds Swagger/OpenAPI documentation with JWT bearer support.</summary>
    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FemVed API",
                Version = "v1",
                Description = "Enterprise-grade women's wellness platform backend API"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your JWT access token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configures JWT Bearer authentication using environment variables.
    /// Reads JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE from configuration.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["JWT_SECRET"]
            ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
        var jwtIssuer = configuration["JWT_ISSUER"] ?? "https://api.femved.com";
        var jwtAudience = configuration["JWT_AUDIENCE"] ?? "https://femved.com";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }

    /// <summary>
    /// Adds authorization policies: AdminOnly, ExpertOrAdmin, and the default authenticated policy.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin"));

            options.AddPolicy("ExpertOrAdmin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Expert", "Admin"));
        });

        return services;
    }
}

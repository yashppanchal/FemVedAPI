using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using DotNetEnv;
using FemVed.API.Extensions;
using FemVed.API.Middleware;
using FemVed.Application.Common.Behaviours;
using FemVed.Infrastructure.Extensions;
using FluentValidation;
using MediatR;
using Serilog;

// ── Load .env.local if it exists (local development only) ────────────────────
// Searches the current directory and all parent directories for .env.local.
// In production (Railway) this file does not exist — real env vars are used instead.
Env.TraversePath().Load(".env.local");

// ── Bootstrap Serilog from appsettings before anything else ──────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting FemVed API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) =>
        lc.ReadFrom.Configuration(ctx.Configuration));

    // ── Controllers ──────────────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ── Swagger ──────────────────────────────────────────────────────────────
    builder.Services.AddSwaggerWithAuth();

    // ── JWT Auth + Authorization Policies ────────────────────────────────────
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationPolicies();

    // ── MediatR + Pipeline Behaviours ────────────────────────────────────────
    // Order: ValidationBehaviour → LoggingBehaviour → PerformanceBehaviour → Handler
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(FemVed.Application.Common.Behaviours.ValidationBehaviour<,>).Assembly);
        cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
        cfg.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
    });

    // ── FluentValidation ─────────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(typeof(FemVed.Application.Common.Behaviours.ValidationBehaviour<,>).Assembly);

    // ── Infrastructure (EF Core, Repositories, UoW) ──────────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Rate Limiting (ASP.NET Core built-in) ────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // General API: 120 requests per minute per IP
        options.AddFixedWindowLimiter("global", limiterOptions =>
        {
            limiterOptions.Window      = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit = 120;
            limiterOptions.QueueLimit  = 0;
        });

        // Auth endpoints: 10 requests per minute per IP (brute-force protection)
        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.Window      = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit = 10;
            limiterOptions.QueueLimit  = 0;
        });
    });

    // ── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("FemVedCors", policy =>
        {
            policy.WithOrigins(
                    builder.Configuration["APP_BASE_URL"] ?? "https://femved.com",
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "https://femvedfrontend.netlify.app/")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    var app = builder.Build();

    // ── Middleware Pipeline ───────────────────────────────────────────────────
    // 1. Correlation ID (first — all subsequent middleware can use it)
    app.UseMiddleware<CorrelationIdMiddleware>();

    // 2. Exception handling (wrap everything below so errors are formatted)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // 3. Request logging
    app.UseMiddleware<RequestLoggingMiddleware>();

    // 4. Swagger (all environments — Railway exposes this behind auth)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FemVed API v1");
        c.RoutePrefix = "swagger";
    });

    // 5. Forwarded headers — must run before HTTPS redirect.
    // Railway (and most cloud proxies) terminate TLS at the edge and forward plain
    // HTTP to the container. Without this, UseHttpsRedirection sees every request
    // as HTTP and issues a redirect loop. This middleware reads X-Forwarded-Proto
    // so ASP.NET Core correctly treats the request as HTTPS.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
    app.UseHttpsRedirection();

    // 6. CORS
    app.UseCors("FemVedCors");

    // 7. Rate limiting
    app.UseRateLimiter();

    // 8. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // 9. Root redirect → Swagger
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    // 10. Health check (outside rate limiter — always reachable by load balancer)
    app.MapHealthChecks("/health");

    // 11. Controllers
    app.MapControllers().RequireRateLimiting("global");

    Log.Information("FemVed API configured successfully");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "FemVed API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Needed for integration test WebApplicationFactory
/// <summary>Entry point partial class for WebApplicationFactory test access.</summary>
public partial class Program { }

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FemVed.API.Middleware;

/// <summary>
/// Middleware that logs each HTTP request and response at Information level using structured logging.
/// Captures method, path, status code, and elapsed time. Does not log request/response bodies
/// to avoid capturing PII or secrets.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>Initializes the middleware.</summary>
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Logs the incoming request and outgoing response.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = context.Items["X-Correlation-Id"]?.ToString() ?? "unknown";

        _logger.LogInformation(
            "HTTP {Method} {Path} started | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation(
                "HTTP {Method} {Path} → {StatusCode} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                correlationId);
        }
    }
}

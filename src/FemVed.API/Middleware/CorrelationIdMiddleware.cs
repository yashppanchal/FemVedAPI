namespace FemVed.API.Middleware;

/// <summary>
/// Middleware that ensures every request carries a <c>X-Correlation-Id</c> header.
/// If the client does not supply one, a new GUID is generated and added to both the
/// request and response headers. Used for distributed tracing in logs.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    /// <summary>Initializes the middleware.</summary>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Invokes the middleware, ensuring a correlation ID is present.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = correlationId;
        }

        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HeaderName] = correlationId.ToString();

        await _next(context);
    }
}

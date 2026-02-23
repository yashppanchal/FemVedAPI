using System.Net;
using FemVed.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ValidationException = FemVed.Domain.Exceptions.ValidationException;

namespace FemVed.API.Middleware;

/// <summary>
/// Global exception handling middleware. Catches all unhandled exceptions and maps them to
/// <see cref="ProblemDetails"/> responses per the FemVed error contract.
/// <list type="bullet">
/// <item><see cref="NotFoundException"/> → 404</item>
/// <item><see cref="ValidationException"/> → 400 with field errors</item>
/// <item><see cref="UnauthorizedException"/> → 401</item>
/// <item><see cref="ForbiddenException"/> → 403</item>
/// <item><see cref="DomainException"/> → 422</item>
/// <item>All others → 500 (exception logged at Error level; generic message returned)</item>
/// </list>
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>Initializes the middleware.</summary>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Executes the next middleware and catches unhandled exceptions.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, title, detail, errors) = exception switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, "Resource Not Found", nfe.Message, (IDictionary<string, string[]>?)null),
            ValidationException ve => (HttpStatusCode.BadRequest, "Validation Failed", ve.Message, ve.Errors),
            UnauthorizedException ue => (HttpStatusCode.Unauthorized, "Unauthorized", ue.Message, null),
            ForbiddenException fe => (HttpStatusCode.Forbidden, "Forbidden", fe.Message, null),
            DomainException de => (HttpStatusCode.UnprocessableEntity, "Domain Rule Violation", de.Message, null),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred. Please try again later.", null)
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        var problem = new ProblemDetails
        {
            Type = $"https://femved.com/errors/{title.ToLowerInvariant().Replace(" ", "-")}",
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}

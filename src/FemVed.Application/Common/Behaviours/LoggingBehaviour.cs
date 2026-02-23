using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that logs request entry and exit at Information level.
/// Logs unhandled exceptions at Error level with full exception detail.
/// Runs after <see cref="ValidationBehaviour{TRequest,TResponse}"/>.
/// Never logs passwords, tokens, or payment card data.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">MediatR response type.</typeparam>
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behaviour with the application logger.</summary>
    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("FemVed Request: {RequestName} started", requestName);

        try
        {
            var response = await next();
            _logger.LogInformation("FemVed Request: {RequestName} completed successfully", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FemVed Request: {RequestName} failed with exception", requestName);
            throw;
        }
    }
}

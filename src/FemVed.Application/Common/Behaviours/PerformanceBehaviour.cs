using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FemVed.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that measures handler execution time.
/// Emits a Warning log if execution exceeds 500 ms so slow queries are visible in structured logs.
/// Runs last in the pipeline.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">MediatR response type.</typeparam>
public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;

    /// <summary>Initializes the behaviour with the application logger.</summary>
    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > WarningThresholdMs)
        {
            _logger.LogWarning(
                "FemVed Performance: {RequestName} took {ElapsedMilliseconds}ms (threshold: {ThresholdMs}ms)",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                WarningThresholdMs);
        }

        return response;
    }
}

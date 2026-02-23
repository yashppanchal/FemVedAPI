using FluentValidation;
using MediatR;
using ValidationException = FemVed.Domain.Exceptions.ValidationException;

namespace FemVed.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs all registered FluentValidation validators for a request
/// before the handler executes. Throws <see cref="Domain.Exceptions.ValidationException"/> if any
/// validation rule fails, which the global exception middleware maps to HTTP 400.
/// Runs first in the pipeline.
/// </summary>
/// <typeparam name="TRequest">MediatR request type.</typeparam>
/// <typeparam name="TResponse">MediatR response type.</typeparam>
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>Initializes the behaviour with all registered validators for <typeparamref name="TRequest"/>.</summary>
    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}

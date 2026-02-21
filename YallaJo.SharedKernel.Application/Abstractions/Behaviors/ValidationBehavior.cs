using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse>(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
                return await next();

            var requestName = typeof(TRequest).Name;
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
            {
                logger.LogWarning(
                    "Validation failed for {RequestName}: {Errors}",
                    requestName,
                    string.Join(", ", failures.Select(f => f.ErrorMessage)));

                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}

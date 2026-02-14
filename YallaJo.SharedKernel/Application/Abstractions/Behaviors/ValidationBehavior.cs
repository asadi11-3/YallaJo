using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors
{
    // ═══════════════════════════════════════
    // Shared.Application/Behaviors/ValidationBehavior.cs
    // ═══════════════════════════════════════

    /// <summary>
    /// Pipeline Behavior للتحقق من صحة Commands
    /// يشتغل قبل Handler
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            _logger.LogInformation(
                "Validating request {RequestName}",
                requestName);

            // ═══════════════════════════════════════
            // لو ما في validators، كمل عادي
            // ═══════════════════════════════════════

            if (!_validators.Any())
            {
                _logger.LogDebug(
                    "No validators found for {RequestName}",
                    requestName);

                return await next();
            }

            // ═══════════════════════════════════════
            // شغل كل الـ Validators
            // ═══════════════════════════════════════

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // ═══════════════════════════════════════
            // اجمع كل الأخطاء
            // ═══════════════════════════════════════

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(failure => failure != null)
                .ToList();

            // ═══════════════════════════════════════
            // لو في أخطاء، ارمي Exception
            // ═══════════════════════════════════════

            if (failures.Any())
            {
                _logger.LogWarning(
                    "Validation failed for {RequestName}: {Errors}",
                    requestName,
                    string.Join(", ", failures.Select(f => f.ErrorMessage)));

                throw new FluentValidation.ValidationException(failures);
            }

            _logger.LogInformation(
                "Validation successful for {RequestName}",
                requestName);

            // ═══════════════════════════════════════
            // كل شي تمام، كمل للـ Handler
            // ═══════════════════════════════════════

            return await next();
        }
    }
}

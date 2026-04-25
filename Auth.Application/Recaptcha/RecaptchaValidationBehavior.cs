using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Recaptcha;

public sealed class RecaptchaValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRecaptchaVerifier _verifier;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<RecaptchaValidationBehavior<TRequest, TResponse>> _logger;

    public RecaptchaValidationBehavior(
        IRecaptchaVerifier verifier,
        IRequestContext requestContext,
        ILogger<RecaptchaValidationBehavior<TRequest, TResponse>> logger)
    {
        _verifier = verifier;
        _requestContext = requestContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRecaptchaProtectedCommand protectedCmd)
            return await next().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(protectedCmd.RecaptchaToken))
        {
            _logger.LogInformation(
                "reCAPTCHA rejected for {Command}: token missing.",
                typeof(TRequest).Name);
            return BuildGenericFailure("Captcha verification failed.");
        }

        if (string.IsNullOrWhiteSpace(protectedCmd.RecaptchaAction))
        {
            _logger.LogError(
                "reCAPTCHA configuration bug for {Command}: expected action is empty.",
                typeof(TRequest).Name);
            return BuildGenericFailure("Captcha verification failed.");
        }

        var verification = await _verifier.VerifyAsync(
            protectedCmd.RecaptchaToken,
            protectedCmd.RecaptchaAction,
            _requestContext.IpAddress,
            cancellationToken).ConfigureAwait(false);

        if (verification.IsFailure)
        {
            _logger.LogInformation(
                "reCAPTCHA rejected for {Command}: {Reason}",
                typeof(TRequest).Name,
                verification.Errors.FirstOrDefault()?.Message ?? "unknown");

            return BuildGenericFailure("Captcha verification failed.");
        }

        return await next().ConfigureAwait(false);
    }

    private static TResponse BuildGenericFailure(string message)
    {
        var responseType = typeof(TResponse);
        var error = Error.Failure("Recaptcha.VerificationFailed", message);
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error, Outcome.Forbidden);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(
                nameof(Result<object>.Failure),
                new[] { typeof(Error), typeof(Outcome) });

            if (failureMethod is not null)
            {
                var failure = failureMethod.Invoke(null, new object[] { error, Outcome.Forbidden });
                if (failure is TResponse typed)
                    return typed;
            }
        }

        throw new InvalidOperationException(
            $"RecaptchaValidationBehavior cannot short-circuit response of type {responseType.FullName}.");
    }
}

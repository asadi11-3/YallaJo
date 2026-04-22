using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Recaptcha;

/// <summary>
/// MediatR pipeline behavior that enforces reCAPTCHA v3 verification on every
/// command that implements <see cref="IRecaptchaProtectedCommand"/>.
///
/// <para>
/// Centralizing the check here removes a class of security bugs: a new
/// sensitive handler cannot forget to call the verifier, and the verifier
/// cannot be bypassed with an alternative code path. If verification fails,
/// the pipeline short-circuits BEFORE the handler is invoked — with a generic
/// failure result so the response never leaks details about why the token
/// was rejected.
/// </para>
/// </summary>
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

    /// <summary>
    /// Builds a generic <c>Outcome.Forbidden</c> result shaped to the request's
    /// declared response type. No details about score / action / Google error
    /// codes are ever surfaced to the client.
    /// </summary>
    private static TResponse BuildGenericFailure(string message)
    {
        var responseType = typeof(TResponse);
        var error = Error.Failure("Recaptcha.VerificationFailed", message);

        // TResponse is always Result or Result<T> for commands via ICommand/ICommandHandler.
        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error, Outcome.Forbidden);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            // Result<T>.Failure(error, Outcome.Forbidden) via reflection.
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

        // Non-Result response — cannot express failure in-band. Surface as an
        // exception so the global handler converts it to a 500 rather than
        // silently letting the request through.
        throw new InvalidOperationException(
            $"RecaptchaValidationBehavior cannot short-circuit response of type {responseType.FullName}.");
    }
}

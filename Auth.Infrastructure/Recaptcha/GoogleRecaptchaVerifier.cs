using System.Net.Http;
using System.Net.Http.Json;
using Auth.Application.Recaptcha;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Infrastructure.Recaptcha;

internal sealed class GoogleRecaptchaVerifier : IRecaptchaVerifier
{
    public const string HttpClientName = "YallaJo.Recaptcha";

    private static readonly Error GenericFailure =
        Error.Failure("Recaptcha.VerificationFailed", "Captcha verification failed.");

    private readonly IHttpClientFactory _httpFactory;
    private readonly RecaptchaOptions _opts;
    private readonly ILogger<GoogleRecaptchaVerifier> _logger;

    public GoogleRecaptchaVerifier(
        IHttpClientFactory httpFactory,
        IOptions<RecaptchaOptions> options,
        ILogger<GoogleRecaptchaVerifier> logger)
    {
        _httpFactory = httpFactory;
        _opts = options.Value;
        _logger = logger;
    }

    public async Task<Result> VerifyAsync(
        string token,
        string expectedAction,
        string? remoteIp = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogInformation("reCAPTCHA: missing token for action {Action}.", expectedAction);
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(expectedAction))
        {
            _logger.LogError("reCAPTCHA: expectedAction is empty. Refusing to verify.");
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        // Intentional escape hatch for non-production test rigs. Must be OFF in prod
        // (the options validator enforces SecretKey when this flag is false).
        if (_opts.BypassForTesting)
            return Result.Success();

        var form = new List<KeyValuePair<string, string>>
        {
            new("secret", _opts.SecretKey),
            new("response", token),
        };
        if (!string.IsNullOrWhiteSpace(remoteIp))
            form.Add(new("remoteip", remoteIp));

        GoogleRecaptchaResponse? response;
        try
        {
            var client = _httpFactory.CreateClient(HttpClientName);
            using var content = new FormUrlEncodedContent(form);
            using var httpResponse = await client
                .PostAsync(_opts.VerifyEndpoint, content, ct)
                .ConfigureAwait(false);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "reCAPTCHA: siteverify returned {StatusCode} for action {Action}.",
                    (int)httpResponse.StatusCode, expectedAction);
                return Result.Failure(GenericFailure, Outcome.Forbidden);
            }

            response = await httpResponse.Content
                .ReadFromJsonAsync<GoogleRecaptchaResponse>(cancellationToken: ct)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Fail closed: bot protection is security-critical; we do NOT let
            // the request through just because Google is unreachable.
            _logger.LogError(ex,
                "reCAPTCHA: siteverify request failed for action {Action}. Failing closed.",
                expectedAction);
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        if (response is null)
        {
            _logger.LogWarning(
                "reCAPTCHA: siteverify returned empty body for action {Action}.", expectedAction);
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        if (!response.Success)
        {
            _logger.LogInformation(
                "reCAPTCHA: verification failed for action {Action}. ErrorCodes={ErrorCodes}",
                expectedAction, string.Join(",", response.ErrorCodes ?? Array.Empty<string>()));
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        if (!string.Equals(response.Action, expectedAction, StringComparison.Ordinal))
        {
            // Action-mismatch = a token minted for one flow is being replayed
            // against another. Critical signal, logged at Warning.
            _logger.LogWarning(
                "reCAPTCHA: action mismatch. Expected={Expected}, Actual={Actual}.",
                expectedAction, response.Action ?? "(null)");
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        var score = response.Score ?? 0.0;
        if (score < _opts.MinimumScore)
        {
            _logger.LogInformation(
                "reCAPTCHA: low score for action {Action}: {Score} < {Threshold}.",
                expectedAction, score, _opts.MinimumScore);
            return Result.Failure(GenericFailure, Outcome.Forbidden);
        }

        return Result.Success();
    }
}

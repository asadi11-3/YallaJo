using System.Text.Json.Serialization;

namespace Auth.Infrastructure.Recaptcha;

/// <summary>
/// Wire model for Google's <c>siteverify</c> JSON response. Fields follow the
/// snake_case names documented at
/// https://developers.google.com/recaptcha/docs/verify.
/// </summary>
internal sealed class GoogleRecaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("score")]
    public double? Score { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("challenge_ts")]
    public DateTime? ChallengeTimestamp { get; init; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; init; }

    [JsonPropertyName("error-codes")]
    public IReadOnlyList<string>? ErrorCodes { get; init; }
}

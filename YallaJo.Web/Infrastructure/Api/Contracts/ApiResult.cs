namespace YallaJo.Web.Infrastructure.Api.Contracts;

/// <summary>
/// Discriminated state for an API call outcome.
/// Controllers switch on this value — never on error strings.
/// </summary>
public enum ApiResultState
{
    /// <summary>HTTP 2xx — operation completed successfully.</summary>
    Success,

    /// <summary>HTTP 401 — token invalid or expired. Sign out and redirect to login.</summary>
    Unauthorized,

    /// <summary>HTTP 403 — user is authenticated but lacks permission. Show AccessDenied.</summary>
    Forbidden,

    /// <summary>HTTP 404 — resource not found.</summary>
    NotFound,

    /// <summary>
    /// HTTP 409 — business rule violation (e.g. duplicate name, protected resource).
    /// result.Error contains the user-facing message from ProblemDetails.
    /// </summary>
    Conflict,

    /// <summary>
    /// HTTP 400 — field-level validation failure.
    /// result.ValidationErrors contains the per-field messages.
    /// </summary>
    ValidationError,

    /// <summary>HTTP 429 — rate limit hit.</summary>
    TooManyRequests,

    /// <summary>Any other non-success response (5xx, unexpected status).</summary>
    Error,
}

/// <summary>
/// Non-generic API result for commands that return no body (POST/PATCH/DELETE → 200 OK).
/// </summary>
public sealed class ApiResult
{
    public bool   IsSuccess { get; private init; }
    public int    StatusCode { get; private init; }

    /// <summary>User-facing message extracted from ProblemDetails.Title or ProblemDetails.Detail.</summary>
    public string? Error { get; private init; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    // ── Convenience booleans (kept for backward compat + simple if-chains) ────
    public bool IsUnauthorized    => StatusCode == 401;
    public bool IsForbidden       => StatusCode == 403;
    public bool IsNotFound        => StatusCode == 404;
    public bool IsConflict        => StatusCode == 409;
    public bool IsValidationError => StatusCode is 400 or 422;
    public bool IsTooManyRequests => StatusCode == 429;

    /// <summary>
    /// Discriminated state for use in switch expressions.
    ///
    /// Controllers should branch like:
    ///   return result.State switch {
    ///       ApiResultState.Success       => RedirectToAction(nameof(Index)),
    ///       ApiResultState.Unauthorized  => RedirectToLogin(),
    ///       ApiResultState.Forbidden     => new ForbidResult(),
    ///       ApiResultState.Conflict      => TempDataError(result.Error),
    ///       ApiResultState.ValidationError => TempDataError("Validation failed"),
    ///       _                            => TempDataError(result.Error),
    ///   };
    /// </summary>
    public ApiResultState State => IsSuccess switch
    {
        true  => ApiResultState.Success,
        false => StatusCode switch
        {
            401         => ApiResultState.Unauthorized,
            403         => ApiResultState.Forbidden,
            404         => ApiResultState.NotFound,
            409         => ApiResultState.Conflict,
            400 or 422  => ApiResultState.ValidationError,
            429         => ApiResultState.TooManyRequests,
            _           => ApiResultState.Error,
        },
    };

    // ── Factory methods ───────────────────────────────────────────────────────

    public static ApiResult Ok(int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiResult Fail(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    public static ApiResult ValidationFail(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = statusCode, ValidationErrors = errors };
}

/// <summary>
/// Generic API result for queries / commands that return a body.
/// </summary>
public sealed class ApiResult<T>
{
    public bool IsSuccess { get; private init; }
    public T?   Data      { get; private init; }
    public int  StatusCode { get; private init; }
    public string? Error  { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    // ── Convenience booleans ──────────────────────────────────────────────────
    public bool IsUnauthorized    => StatusCode == 401;
    public bool IsForbidden       => StatusCode == 403;
    public bool IsNotFound        => StatusCode == 404;
    public bool IsConflict        => StatusCode == 409;
    public bool IsValidationError => StatusCode is 400 or 422;
    public bool IsTooManyRequests => StatusCode == 429;

    /// <summary>Discriminated state. See <see cref="ApiResult.State"/> for usage guidance.</summary>
    public ApiResultState State => IsSuccess switch
    {
        true  => ApiResultState.Success,
        false => StatusCode switch
        {
            401         => ApiResultState.Unauthorized,
            403         => ApiResultState.Forbidden,
            404         => ApiResultState.NotFound,
            409         => ApiResultState.Conflict,
            400 or 422  => ApiResultState.ValidationError,
            429         => ApiResultState.TooManyRequests,
            _           => ApiResultState.Error,
        },
    };

    // ── Factory methods ───────────────────────────────────────────────────────

    public static ApiResult<T> Ok(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    public static ApiResult<T> ValidationFail(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = statusCode, ValidationErrors = errors };
}

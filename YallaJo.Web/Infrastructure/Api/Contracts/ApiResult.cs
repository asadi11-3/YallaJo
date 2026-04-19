namespace YallaJo.Web.Infrastructure.Api.Contracts;

public enum ApiResultState
{
    Success,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    ValidationError,
    TooManyRequests,
    Error,
}

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

    public static ApiResult Ok(int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiResult Fail(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    public static ApiResult ValidationFail(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = statusCode, ValidationErrors = errors };

    // Convenience factories
    public static ApiResult Fail(string error) => Fail(500, error);
    public static ApiResult Invalid(IReadOnlyDictionary<string, string[]> errors) => ValidationFail(400, errors);
    public static ApiResult ForceSignOut() => Fail(401, "Session expired.");
    public bool RequireSignOut => IsUnauthorized;
    public string? Message => Error;

    public static ApiResult<T> Ok<T>(T data, int statusCode = 200) =>
        ApiResult<T>.Ok(data, statusCode);

    public static ApiResult<T> Fail<T>(int statusCode, string? error = null) =>
        ApiResult<T>.Fail(statusCode, error);

    public static ApiResult<T> ValidationFail<T>(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        ApiResult<T>.ValidationFail(statusCode, errors);
}

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
    public bool RequireSignOut    => IsUnauthorized;

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


    public static ApiResult<T> Ok(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    public static ApiResult<T> ValidationFail(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = statusCode, ValidationErrors = errors };

    public static ApiResult<T> ForceSignOut() => Fail(401, "Session expired.");
    public static ApiResult<T> CreateSuccess(T data, int statusCode) => Ok(data, statusCode);
    public static ApiResult<T> CreateSuccess(T data) => Ok(data);
    public static ApiResult<T> CreateFailure(int statusCode, string? error = null) => Fail(statusCode, error);
    public static ApiResult<T> CreateFailure(string error) => Fail(500, error);
    public static ApiResult<T> CreateValidationFailure(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        ValidationFail(statusCode, errors);
}

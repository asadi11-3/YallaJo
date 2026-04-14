namespace YallaJo.Web.Infrastructure.Api.Contracts;

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


    public static ApiResult<T> Ok<T>(T data, int statusCode = 200) =>
        ApiResult<T>.CreateSuccess(data, statusCode);

    public static ApiResult<T> Fail<T>(int statusCode, string? error = null) =>
        ApiResult<T>.CreateFailure(statusCode, error);

    public static ApiResult<T> ValidationFail<T>(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        ApiResult<T>.CreateValidationFailure(statusCode, errors);
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


    internal static ApiResult<T> CreateSuccess(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    internal static ApiResult<T> CreateFailure(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    internal static ApiResult<T> CreateValidationFailure(int statusCode, IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = statusCode, ValidationErrors = errors };
}

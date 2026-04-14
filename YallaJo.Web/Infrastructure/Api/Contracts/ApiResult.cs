namespace YallaJo.Web.Infrastructure.Api.Contracts;

public sealed class ApiResult
{
    public bool IsSuccess { get; private init; }
    public int StatusCode { get; private init; }
    public string? Error { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsNotFound => StatusCode == 404;
    public bool IsConflict => StatusCode == 409;
    public bool IsValidationError => StatusCode == 422;
    public bool IsTooManyRequests => StatusCode == 429;

    public static ApiResult Ok(int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiResult Fail(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    public static ApiResult ValidationFail(IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = 422, ValidationErrors = errors };
}


public sealed class ApiResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public int StatusCode { get; private init; }
    public string? Error { get; private init; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; private init; }

    public bool IsUnauthorized => StatusCode == 401;
    public bool IsForbidden => StatusCode == 403;
    public bool IsNotFound => StatusCode == 404;
    public bool IsConflict => StatusCode == 409;
    public bool IsValidationError => StatusCode == 422;
    public bool IsTooManyRequests => StatusCode == 429;

    public static ApiResult<T> Ok(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(int statusCode, string? error = null) =>
        new() { IsSuccess = false, StatusCode = statusCode, Error = error };

    public static ApiResult<T> ValidationFail(IReadOnlyDictionary<string, string[]> errors) =>
        new() { IsSuccess = false, StatusCode = 422, ValidationErrors = errors };
}

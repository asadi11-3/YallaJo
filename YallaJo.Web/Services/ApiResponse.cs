namespace YallaJo.Web.Services;

/// <summary>
/// Wraps an API response with success/error state for use in MVC controllers.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public int StatusCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ApiResponse<T> Success(T data, int statusCode = 200) =>
        new() { IsSuccess = true, Data = data, StatusCode = statusCode };

    public static ApiResponse<T> Failure(int statusCode, string? errorMessage) =>
        new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
}

/// <summary>
/// Non-generic version for void API calls (DELETE, etc.).
/// </summary>
public sealed class ApiResponse
{
    public bool IsSuccess { get; private init; }
    public int StatusCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ApiResponse Success(int statusCode = 200) =>
        new() { IsSuccess = true, StatusCode = statusCode };

    public static ApiResponse Failure(int statusCode, string? errorMessage) =>
        new() { IsSuccess = false, StatusCode = statusCode, ErrorMessage = errorMessage };
}

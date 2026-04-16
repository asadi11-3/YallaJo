using System.Text;
using System.Text.Json;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Services;

public sealed class ApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions SerializeOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions DeserializeOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ApiClient(HttpClient http) => _http = http;

    public async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(path, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        using var content  = ToJson(body);
        using var response = await _http.PostAsync(path, content, ct);
        return await ReadAsync<T>(response, ct);
    }

    public async Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        using var content  = ToJson(body);
        using var response = await _http.PostAsync(path, content, ct);
        return await ReadNoBodyAsync(response, ct);
    }

    public async Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = ToJson(body),
        };
        using var response = await _http.SendAsync(request, ct);
        return await ReadNoBodyAsync(response, ct);
    }

    public async Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(path, ct);
        return await ReadNoBodyAsync(response, ct);
    }

    private static StringContent? ToJson(object? body)
    {
        if (body is null) return null;
        return new StringContent(
            JsonSerializer.Serialize(body, SerializeOpts),
            Encoding.UTF8,
            "application/json");
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ApiResult<T>.CreateFailure((int)response.StatusCode, "Empty response body.");
            }

            try
            {
                var data = JsonSerializer.Deserialize<T>(raw, DeserializeOpts);
                return data is null
                    ? ApiResult<T>.CreateFailure((int)response.StatusCode, "Could not parse response body.")
                    : ApiResult<T>.CreateSuccess(data, (int)response.StatusCode);
            }
            catch (JsonException)
            {
                return ApiResult<T>.CreateFailure((int)response.StatusCode, "Could not parse response body.");
            }
        }

        return ParseError<T>((int)response.StatusCode, raw);
    }

    private static async Task<ApiResult> ReadNoBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return ApiResult.Ok((int)response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync(ct);
        var err = ParseError<object>((int)response.StatusCode, raw);
        return err.IsValidationError
            ? ApiResult.ValidationFail(err.StatusCode, err.ValidationErrors!)
            : ApiResult.Fail(err.StatusCode, err.Error);
    }

    private static ApiResult<T> ParseError<T>(int statusCode, string raw)
    {
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetails>(raw, DeserializeOpts);

            // 400 = FluentValidation errors (Outcome.Invalid = 400 on the backend)
            // 422 = semantic validation (kept for defensive compatibility)
            if (statusCode is 400 or 422 && problem?.Errors?.Count > 0)
            {
                var errors = problem.Errors
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToArray());
                return ApiResult<T>.CreateValidationFailure(statusCode, errors);
            }

            var msg = problem?.Title ?? problem?.Detail ?? $"HTTP {statusCode}";
            return ApiResult<T>.CreateFailure(statusCode, msg);
        }
        catch
        {
            return ApiResult<T>.CreateFailure(statusCode, $"HTTP {statusCode}");
        }
    }

    private sealed class ProblemDetails
    {
        public string? Title  { get; init; }
        public string? Detail { get; init; }
        public Dictionary<string, List<string>>? Errors { get; init; }
    }
}

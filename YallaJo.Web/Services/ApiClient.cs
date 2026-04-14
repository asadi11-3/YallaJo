using System.Text;
using System.Text.Json;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Services;

/// <summary>
/// Typed HTTP client registered via AddHttpClient&lt;ApiClient&gt; in Program.cs.
/// All outbound API calls go through this class; the JwtAuthHandler
/// DelegatingHandler transparently attaches the Bearer token.
///
/// Caller never touches HttpClient directly — only ApiClient methods.
/// Returns ApiResult / ApiResult&lt;T&gt; so callers can pattern-match outcomes
/// without catching exceptions.
/// </summary>
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

    // ── GET ─────────────────────────────────────────────────────────────────

    public async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        using var response = await _http.GetAsync(path, ct);
        return await ReadAsync<T>(response, ct);
    }

    // ── POST ────────────────────────────────────────────────────────────────

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

    // ── PATCH ───────────────────────────────────────────────────────────────

    public async Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = ToJson(body),
        };
        using var response = await _http.SendAsync(request, ct);
        return await ReadNoBodyAsync(response, ct);
    }

    // ── DELETE ──────────────────────────────────────────────────────────────

    public async Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(path, ct);
        return await ReadNoBodyAsync(response, ct);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

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
            var data = JsonSerializer.Deserialize<T>(raw, DeserializeOpts);
            return ApiResult<T>.Ok(data!, (int)response.StatusCode);
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
            ? ApiResult.ValidationFail(err.ValidationErrors!)
            : ApiResult.Fail(err.StatusCode, err.Error);
    }

    private static ApiResult<T> ParseError<T>(int statusCode, string raw)
    {
        try
        {
            var problem = JsonSerializer.Deserialize<ProblemDetails>(raw, DeserializeOpts);

            if (statusCode == 422 && problem?.Errors?.Count > 0)
            {
                var errors = problem.Errors
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToArray());
                return ApiResult<T>.ValidationFail(errors);
            }

            var msg = problem?.Title ?? problem?.Detail ?? $"HTTP {statusCode}";
            return ApiResult<T>.Fail(statusCode, msg);
        }
        catch
        {
            return ApiResult<T>.Fail(statusCode, $"HTTP {statusCode}");
        }
    }

   
    private sealed class ProblemDetails
    {
        public string? Title  { get; init; }
        public string? Detail { get; init; }
        public Dictionary<string, List<string>>? Errors { get; init; }
    }
}

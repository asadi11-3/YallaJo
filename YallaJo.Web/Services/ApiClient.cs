using System.Net.Http.Json;

namespace YallaJo.Web.Services;

/// <summary>
/// Typed HttpClient that communicates with the YallaJo API.
/// JWT is automatically attached via <see cref="JwtAuthHandler"/>.
/// </summary>
public sealed class ApiClient(HttpClient http)
{
    // ── GET ──────────────────────────────────────────────────────────────
    public async Task<ApiResponse<T>> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await http.GetAsync(url, ct);
        return await ParseResponse<T>(response, ct);
    }

    // ── POST ─────────────────────────────────────────────────────────────
    public async Task<ApiResponse<T>> PostAsync<T>(string url, object? payload = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(url, payload, ct);
        return await ParseResponse<T>(response, ct);
    }

    public async Task<ApiResponse> PostAsync(string url, object? payload = null, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(url, payload, ct);
        return await ParseVoidResponse(response, ct);
    }

    // ── PUT ──────────────────────────────────────────────────────────────
    public async Task<ApiResponse<T>> PutAsync<T>(string url, object? payload = null, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(url, payload, ct);
        return await ParseResponse<T>(response, ct);
    }

    public async Task<ApiResponse> PutAsync(string url, object? payload = null, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync(url, payload, ct);
        return await ParseVoidResponse(response, ct);
    }

    // ── DELETE ────────────────────────────────────────────────────────────
    public async Task<ApiResponse> DeleteAsync(string url, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(url, ct);
        return await ParseVoidResponse(response, ct);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync(url, ct);
        return await ParseResponse<T>(response, ct);
    }

    // ── Multipart (file upload) ──────────────────────────────────────────
    public async Task<ApiResponse<T>> PostMultipartAsync<T>(
        string url,
        MultipartFormDataContent content,
        CancellationToken ct = default)
    {
        var response = await http.PostAsync(url, content, ct);
        return await ParseResponse<T>(response, ct);
    }

    // ── Response parsing ─────────────────────────────────────────────────

    private static async Task<ApiResponse<T>> ParseResponse<T>(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(ct);
            return ApiResponse<T>.Success(data!, (int)response.StatusCode);
        }

        var error = await ReadErrorMessage(response, ct);
        return ApiResponse<T>.Failure((int)response.StatusCode, error);
    }

    private static async Task<ApiResponse> ParseVoidResponse(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return ApiResponse.Success((int)response.StatusCode);

        var error = await ReadErrorMessage(response, ct);
        return ApiResponse.Failure((int)response.StatusCode, error);
    }

    private static async Task<string> ReadErrorMessage(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailDto>(ct);
            return problem?.Detail
                ?? problem?.Title
                ?? $"Request failed with status {(int)response.StatusCode}.";
        }
        catch
        {
            return $"Request failed with status {(int)response.StatusCode}.";
        }
    }

    /// <summary>Maps the RFC 7807 ProblemDetails shape returned by the API.</summary>
    private sealed record ProblemDetailDto(int? Status, string? Title, string? Detail);
}

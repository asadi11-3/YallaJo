using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Services;

public sealed class ApiClient : IApiClient
{
    /// <summary>
    /// HTTP status used by the BFF when the API itself is unreachable or
    /// produced a malformed response — <c>503 Service Unavailable</c> matches
    /// the semantics that every <see cref="ApiResult"/> consumer already
    /// handles via the generic "Error" branch, and keeps the stack trace out
    /// of the response shown to the user.
    /// </summary>
    private const int ServiceUnavailableStatusCode = (int)HttpStatusCode.ServiceUnavailable;

    /// <summary>
    /// Generic message used for every transport-layer failure. Intentionally
    /// free of infrastructure details (hostnames, ports, socket error codes)
    /// so the UI does not leak the API's address to end users.
    /// </summary>
    private const string ServiceUnavailableMessage =
        "The service is temporarily unavailable. Please try again in a moment.";

    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions SerializeOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions DeserializeOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Single constructor so AddHttpClient<ApiClient>(...) can pick it
    // unambiguously via ActivatorUtilities. An earlier variant kept a
    // 1-arg convenience overload "for tests", which was rejected at runtime
    // with "Multiple constructors accepting all given argument types …"
    // because the DI container could satisfy both ctors (ILogger<T> is
    // always available). Tests that need to instantiate ApiClient directly
    // pass NullLogger<ApiClient>.Instance.
    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default) =>
        SendWithBodyAsync<T>(path, () => _http.GetAsync(path, ct), ct);

    public async Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await _http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var raw = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                return ParseError<ApiFile>((int)response.StatusCode, raw);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? "download";

            return ApiResult<ApiFile>.CreateSuccess(
                new ApiFile(bytes, contentType, fileName),
                (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "API file download from {Path} failed at the transport layer; returning 503 to caller.",
                path);
            return ApiResult<ApiFile>.Fail(ServiceUnavailableStatusCode, ServiceUnavailableMessage);
        }
        finally
        {
            response?.Dispose();
        }
    }

    public Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default) =>
        SendWithBodyAsync<T>(path, async () =>
        {
            using var content = ToJson(body);
            return await _http.PostAsync(path, content, ct);
        }, ct);

    public Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default) =>
        SendNoBodyAsync(path, async () =>
        {
            using var content = ToJson(body);
            return await _http.PostAsync(path, content, ct);
        }, ct);

    public Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default) =>
        SendNoBodyAsync(path, async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Patch, path)
            {
                Content = ToJson(body),
            };
            return await _http.SendAsync(request, ct);
        }, ct);

    public Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default) =>
        SendWithBodyAsync<T>(path, async () =>
        {
            using var content = ToJson(body);
            return await _http.PutAsync(path, content, ct);
        }, ct);

    public Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default) =>
        SendNoBodyAsync(path, async () =>
        {
            using var content = ToJson(body);
            return await _http.PutAsync(path, content, ct);
        }, ct);

    public Task<ApiResult<T>> PutFileAsync<T>(
        string path,
        Stream fileStream,
        string fileName,
        string contentType,
        string formFieldName = "file",
        CancellationToken ct = default) =>
        SendWithBodyAsync<T>(path, async () =>
        {
            using var multipart = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            multipart.Add(streamContent, formFieldName, fileName);

            using var request = new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = multipart,
            };
            return await _http.SendAsync(request, ct);
        }, ct);

    public Task<ApiResult<T>> PostFileAsync<T>(
        string path,
        Stream fileStream,
        string fileName,
        string contentType,
        IReadOnlyDictionary<string, string>? formFields = null,
        string formFieldName = "file",
        CancellationToken ct = default) =>
        SendWithBodyAsync<T>(path, async () =>
        {
            using var multipart = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            multipart.Add(streamContent, formFieldName, fileName);

            if (formFields is not null)
            {
                foreach (var kvp in formFields)
                {
                    multipart.Add(new StringContent(kvp.Value), kvp.Key);
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = multipart,
            };
            return await _http.SendAsync(request, ct);
        }, ct);

    public Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default) =>
        SendNoBodyAsync(path, () => _http.DeleteAsync(path, ct), ct);

    public Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default) =>
        SendNoBodyAsync(path, async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, path)
            {
                Content = ToJson(body),
            };
            return await _http.SendAsync(request, ct);
        }, ct);

    private async Task<ApiResult<T>> SendWithBodyAsync<T>(
        string path,
        Func<Task<HttpResponseMessage>> send,
        CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await send().ConfigureAwait(false);
            return await ReadAsync<T>(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "API call to {Path} failed at the transport layer; returning 503 to caller.",
                path);
            return ApiResult<T>.Fail(ServiceUnavailableStatusCode, ServiceUnavailableMessage);
        }
        finally
        {
            response?.Dispose();
        }
    }

    private async Task<ApiResult> SendNoBodyAsync(
        string path,
        Func<Task<HttpResponseMessage>> send,
        CancellationToken ct)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await send().ConfigureAwait(false);
            return await ReadNoBodyAsync(response, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsTransportFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "API call to {Path} failed at the transport layer; returning 503 to caller.",
                path);
            return ApiResult.Fail(ServiceUnavailableStatusCode, ServiceUnavailableMessage);
        }
        finally
        {
            response?.Dispose();
        }
    }

    /// <summary>
    /// True for exceptions that represent the BFF being unable to reach the API
    /// or consume its response (as opposed to application-level HTTP errors,
    /// which <see cref="HttpResponseMessage.IsSuccessStatusCode"/> already
    /// reflects and <c>ReadAsync</c> converts to <see cref="ApiResult"/>s).
    /// </summary>
    private static bool IsTransportFailure(Exception ex) =>
        ex is HttpRequestException
           or System.Net.Sockets.SocketException
           or IOException
           or TaskCanceledException        // HttpClient timeout surfaces as TaskCanceledException
           or TimeoutException;

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

            if (statusCode is 400 or 422 && problem?.Errors?.Count > 0)
            {
                var errors = problem.Errors
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToArray());
                return ApiResult<T>.CreateValidationFailure(statusCode, errors);
            }

            // ── (2) SharedKernel single-error Problem with "Validation.<Field>" title ──
            if (statusCode is 400 or 422
                && problem?.Title is { } title
                && title.StartsWith("Validation.", StringComparison.Ordinal))
            {
                var field = title["Validation.".Length..];
                if (!string.IsNullOrWhiteSpace(field))
                {
                    var message = !string.IsNullOrWhiteSpace(problem.Detail) ? problem.Detail! : title;
                    var single  = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                    {
                        [field] = [message],
                    };
                    return ApiResult<T>.CreateValidationFailure(statusCode, single);
                }
            }

            // ── (3) Any other failure: prefer Detail (human) over Title (code). ─
            var msg = !string.IsNullOrWhiteSpace(problem?.Detail) ? problem!.Detail!
                    : !string.IsNullOrWhiteSpace(problem?.Title)  ? problem!.Title!
                    : $"HTTP {statusCode}";

            // Append correlation id on server errors so admins can grep logs.
            if (statusCode >= 500 && !string.IsNullOrWhiteSpace(problem?.CorrelationId))
                msg = $"{msg} [correlationId={problem!.CorrelationId}]";

            return ApiResult<T>.CreateFailure(statusCode, msg);
        }
        catch (JsonException)
        {
            // Body wasn't JSON — surface a short snippet so admins aren't blind.
            var snippet = string.IsNullOrWhiteSpace(raw)
                ? $"HTTP {statusCode}"
                : $"HTTP {statusCode}: {raw[..Math.Min(raw.Length, 200)]}";
            return ApiResult<T>.CreateFailure(statusCode, snippet);
        }
    }

    private sealed class ProblemDetails
    {
        public string? Title  { get; init; }
        public string? Detail { get; init; }
        public Dictionary<string, List<string>>? Errors { get; init; }

        public string? CorrelationId { get; init; }
    }
}

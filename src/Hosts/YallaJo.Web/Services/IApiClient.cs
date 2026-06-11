using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Services;

/// <summary>
/// Abstraction over the BFF's typed HTTP client to the YallaJo API.
/// <para>
/// The concrete <see cref="ApiClient"/> already provides generic, verb-based
/// endpoint consumption returning <see cref="ApiResult"/>/<see cref="ApiResult{T}"/>.
/// This interface exists purely to give feature-level API clients
/// (e.g. <c>UsersApiClient</c>, <c>ProfileApiClient</c>) a mockable seam so they
/// can be unit-tested without a real <see cref="System.Net.Http.HttpClient"/> /
/// <see cref="System.Net.Http.HttpMessageHandler"/>.
/// </para>
/// </summary>
public interface IApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default);

    /// <summary>
    /// GET a binary payload (e.g. a rendered PDF) and return its bytes,
    /// content type and suggested file name. Used to proxy file downloads
    /// (such as invoice PDFs) through the BFF so the JWT never leaves the server.
    /// </summary>
    Task<ApiResult<ApiFile>> GetFileAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// GET a downloadable payload as a live response stream. Callers must return
    /// or dispose the stream; it owns the underlying HTTP response lifetime.
    /// </summary>
    Task<ApiResult<ApiStream>> GetStreamAsync(string path, CancellationToken ct = default) =>
        Task.FromResult(ApiResult<ApiStream>.Fail(501, "Streaming downloads are not supported by this API client."));

    Task<ApiResult<T>> PostAsync<T>(string path, object? body = null, CancellationToken ct = default);

    Task<ApiResult> PostAsync(string path, object? body = null, CancellationToken ct = default);

    Task<ApiResult> PatchAsync(string path, object? body = null, CancellationToken ct = default);

    Task<ApiResult<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default);

    Task<ApiResult> PutAsync(string path, object? body = null, CancellationToken ct = default);

    Task<ApiResult<T>> PutFileAsync<T>(
        string path,
        Stream fileStream,
        string fileName,
        string contentType,
        IReadOnlyDictionary<string, string>? formFields = null,
        string formFieldName = "file",
        CancellationToken ct = default);

    Task<ApiResult<T>> PostFileAsync<T>(
        string path,
        Stream fileStream,
        string fileName,
        string contentType,
        IReadOnlyDictionary<string, string>? formFields = null,
        string formFieldName = "file",
        CancellationToken ct = default);

    /// <summary>
    /// POST several files in a single multipart request (e.g. bulk image upload, API7).
    /// All files are sent under the same <paramref name="formFieldName"/>. Default
    /// implementation returns 501 so existing test doubles keep compiling; the concrete
    /// <see cref="ApiClient"/> overrides it.
    /// </summary>
    Task<ApiResult<T>> PostFilesAsync<T>(
        string path,
        IReadOnlyList<ApiUploadFile> files,
        IReadOnlyDictionary<string, string>? formFields = null,
        string formFieldName = "files",
        CancellationToken ct = default) =>
        Task.FromResult(ApiResult<T>.Fail(501, "Multi-file upload is not supported by this API client."));

    Task<ApiResult> DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// DELETE with a JSON request body. Required by endpoints that take a body on
    /// delete (e.g. Blog delete/unlink, which carry an optimistic-concurrency
    /// <c>RowVersion</c> in <c>BlogRowVersionRequest</c>).
    /// </summary>
    Task<ApiResult> DeleteAsync(string path, object? body, CancellationToken ct = default);
}

namespace YallaJo.Web.Infrastructure.Api.Contracts;

/// <summary>
/// A binary payload proxied from the API (bytes + content type + file name).
/// Returned by <c>IApiClient.GetFileAsync</c> for downloads such as invoice PDFs.
/// </summary>
public sealed record ApiFile(byte[] Content, string ContentType, string FileName);

/// <summary>
/// One file to forward to the API in a multipart batch upload
/// (<c>IApiClient.PostFilesAsync</c>). The <see cref="Stream"/> is owned by the caller
/// and is read once during the request.
/// </summary>
public sealed record ApiUploadFile(Stream Stream, string FileName, string ContentType);

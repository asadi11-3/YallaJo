namespace YallaJo.Web.Infrastructure.Api.Contracts;

/// <summary>
/// A binary payload proxied from the API (bytes + content type + file name).
/// Returned by <c>IApiClient.GetFileAsync</c> for downloads such as invoice PDFs.
/// </summary>
public sealed record ApiFile(byte[] Content, string ContentType, string FileName);

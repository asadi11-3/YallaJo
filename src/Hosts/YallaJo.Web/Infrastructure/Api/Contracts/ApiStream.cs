namespace YallaJo.Web.Infrastructure.Api.Contracts;

/// <summary>
/// Returned by <c>IApiClient.GetStreamAsync</c> for downloads that must be proxied without buffering.
/// Disposing <see cref="Content"/> releases the underlying HTTP response.
/// </summary>
public sealed record ApiStream(Stream Content, string ContentType, string FileName);

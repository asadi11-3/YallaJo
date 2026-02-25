using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Application.Abstractions.Context;

namespace YallaJo.Api.Services;

/// <summary>
/// Extracts HTTP request metadata from the current HttpContext.
/// </summary>
public sealed class RequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    private HttpContext? Context => accessor.HttpContext;

    public string? IpAddress =>
        Context?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        Context?.Request.Headers.UserAgent.ToString();

    public string? DeviceName =>
        Context?.Request.Headers["X-Device-Name"].ToString();

    public string? AcceptLanguage =>
        Context?.Request.Headers.AcceptLanguage.ToString();

    public string? Platform =>
        Context?.Request.Headers["X-Platform"].ToString();
}

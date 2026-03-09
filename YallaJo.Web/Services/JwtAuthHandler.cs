using System.Net.Http.Headers;

namespace YallaJo.Web.Services;

/// <summary>
/// DelegatingHandler that automatically attaches the JWT token (stored in the
/// cookie session) to every outgoing HttpClient request as a Bearer token.
/// </summary>
public sealed class JwtAuthHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var jwt = httpContextAccessor.HttpContext?.User?.FindFirst("jwt")?.Value;

        if (jwt is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        return base.SendAsync(request, cancellationToken);
    }
}

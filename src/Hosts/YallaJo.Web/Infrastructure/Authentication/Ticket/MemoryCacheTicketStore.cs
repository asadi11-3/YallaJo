using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace YallaJo.Web.Infrastructure.Authentication.Ticket;

/// <summary>
/// Server-side <see cref="ITicketStore"/> for the main application cookie (BFF pattern).
/// <para>
/// The browser cookie then carries only a small opaque session key; the full
/// <see cref="AuthenticationTicket"/> — including the embedded JWT access token and
/// refresh token (Phase 1 minimal principal) — lives server-side in
/// <see cref="IMemoryCache"/>. This keeps the cookie tiny (1 chunk) and well under
/// Kestrel's request-header limit, and keeps the tokens entirely off the browser.
/// </para>
/// <para>
/// LIMITATIONS:
/// <list type="bullet">
///   <item>IMemoryCache is process-local → suitable for SINGLE-INSTANCE deployments only.</item>
///   <item>Sessions are LOST when the Web app restarts (the cache is in-memory). Users
///         are then treated as unauthenticated and redirected to sign-in — expected.</item>
///   <item>For multi-instance / load-balanced hosting, replace this with a distributed
///         implementation backed by <c>IDistributedCache</c> (e.g. Redis).</item>
/// </list>
/// </para>
/// </summary>
public sealed class MemoryCacheTicketStore : ITicketStore
{
    private const string KeyPrefix = "auth-ticket:";

    // Fallback lifetime used only when a ticket has no explicit ExpiresUtc. Aligned
    // with the cookie's ExpireTimeSpan (8h) configured in Program.cs.
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);

    private readonly IMemoryCache _cache;

    public MemoryCacheTicketStore(IMemoryCache cache) => _cache = cache;

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = KeyPrefix + GenerateKey();
        await RenewAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var options = new MemoryCacheEntryOptions();

        // Honour the ticket's own expiry when present; otherwise fall back to the
        // default lifetime so an entry can never live forever.
        var expiresUtc = ticket.Properties.ExpiresUtc;
        if (expiresUtc.HasValue)
            options.SetAbsoluteExpiration(expiresUtc.Value);
        else
            options.SetAbsoluteExpiration(DefaultLifetime);

        _cache.Set(key, ticket, options);
        return Task.CompletedTask;
    }

    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        // Returns null when missing or expired → the cookie middleware then treats the
        // request as unauthenticated and the user is redirected to sign-in.
        _cache.TryGetValue(key, out AuthenticationTicket? ticket);
        return Task.FromResult(ticket);
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    private static string GenerateKey()
    {
        // Cryptographically strong, URL-safe opaque key (no user data encoded).
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}

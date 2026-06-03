using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace YallaJo.Web.Infrastructure.Authentication.Ticket;

/// <summary>
/// Attaches the server-side <see cref="MemoryCacheTicketStore"/> to the MAIN application
/// cookie only (scheme <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/>).
/// <para>
/// Implemented as <see cref="IPostConfigureOptions{TOptions}"/> so the ticket store can be
/// resolved from DI (the <c>AddCookie</c> lambda in Program.cs runs at configuration time
/// and cannot resolve scoped/registered services). Other cookie schemes — notably the
/// external-auth intermediate cookie (<c>ExternalProviderConstants.ExternalSignInScheme</c>)
/// and OAuth correlation cookies — are intentionally left untouched.
/// </para>
/// </summary>
public sealed class ConfigureCookieTicketStore : IPostConfigureOptions<CookieAuthenticationOptions>
{
    private readonly MemoryCacheTicketStore _ticketStore;

    public ConfigureCookieTicketStore(MemoryCacheTicketStore ticketStore) =>
        _ticketStore = ticketStore;

    public void PostConfigure(string? name, CookieAuthenticationOptions options)
    {
        // Only the main application cookie uses the server-side ticket store.
        if (name == CookieAuthenticationDefaults.AuthenticationScheme)
            options.SessionStore = _ticketStore;
    }
}

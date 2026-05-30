using Auth.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Services;

internal sealed class InviteLinkBuilder(IOptions<InviteOptions> options) : IInviteLinkBuilder
{
    private readonly InviteOptions _opts = options.Value;

    public string Build(string email, string plainToken)
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(plainToken);

        return _opts.AcceptUrlTemplate
            .Replace("{email}", encodedEmail, StringComparison.Ordinal)
            .Replace("{token}", encodedToken, StringComparison.Ordinal);
    }
}

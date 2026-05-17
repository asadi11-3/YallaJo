using Analytics.Contracts.Services;

namespace Analytics.Infrastructure.Services;

internal sealed class NoopClientContextProvider : IClientContextProvider
{
    public ClientContext GetCurrent() => new(null, null, null, null);
}

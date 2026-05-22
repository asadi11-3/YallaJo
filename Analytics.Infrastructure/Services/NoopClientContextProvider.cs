using Analytics.Contracts.Services;

namespace Analytics.Infrastructure.Services;

internal sealed class NoopClientContextProvider : IClientContextProvider
{
    public string? UserAgent => null;
    public string? IpAddress => null;
    public string? CorrelationId => null;
}

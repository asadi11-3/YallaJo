namespace Analytics.Contracts.Services;

public interface IClientContextProvider
{
    ClientContext GetCurrent();
}

public sealed record ClientContext(string? IpAddress, string? UserAgent, string? SessionId, string? DeviceType);

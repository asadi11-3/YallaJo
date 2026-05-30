namespace Analytics.Contracts.Services;

public interface IClientContextProvider
{
    string? UserAgent { get; }
    string? IpAddress { get; }
    string? CorrelationId { get; }
}

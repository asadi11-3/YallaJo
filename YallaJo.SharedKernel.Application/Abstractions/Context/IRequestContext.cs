namespace YallaJo.SharedKernel.Application.Abstractions.Context;

/// <summary>
/// HTTP request metadata extracted from the current HttpContext.
/// Registered as scoped in the API host.
/// </summary>
public interface IRequestContext
{
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? DeviceName { get; }
    string? AcceptLanguage { get; }
    string? Platform { get; }
}

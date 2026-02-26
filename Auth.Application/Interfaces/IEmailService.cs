namespace Auth.Application.Interfaces;

/// <summary>
/// Sends transactional emails (OTP codes, notifications).
/// Implemented by GmailEmailService in Auth.Infrastructure.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

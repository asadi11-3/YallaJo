namespace Auth.Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct = default);
}

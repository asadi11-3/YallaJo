using System.Net;
using System.Net.Mail;
using Auth.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Sends emails via Gmail SMTP using App Password authentication.
/// Port 587, TLS enabled.
/// </summary>
internal sealed class GmailEmailService(
    IOptions<GmailOptions> options,
    ILogger<GmailEmailService> logger) : IEmailService
{
    private readonly GmailOptions _opts = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(_opts.SenderEmail, _opts.AppPassword),
            EnableSsl = true
        };

        var message = new MailMessage(
            from: _opts.SenderEmail,
            to: to,
            subject: subject,
            body: body)
        {
            IsBodyHtml = false
        };

        logger.LogInformation("Sending email to {To} with subject '{Subject}'", to, subject);

        await client.SendMailAsync(message, ct);

        logger.LogInformation("Email sent successfully to {To}", to);
    }
}

using Messaging.Contracts.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;

namespace Messaging.Infrastructure.Services;

/// <summary>SMTP email sender via MailKit. Configured via Messaging:Email:Smtp section.</summary>
internal sealed class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            var host = configuration["Messaging:Email:Smtp:Host"] ?? "localhost";
            var port = int.TryParse(configuration["Messaging:Email:Smtp:Port"], out var p) ? p : 587;
            var user = configuration["Messaging:Email:Smtp:Username"] ?? string.Empty;
            var pass = configuration["Messaging:Email:Smtp:Password"] ?? string.Empty;
            var fromEmail = configuration["Messaging:Email:Smtp:FromAddress"] ?? "noreply@yallajo.com";
            var fromName = configuration["Messaging:Email:Smtp:FromName"] ?? "YallaJo";
            var useSsl = bool.TryParse(configuration["Messaging:Email:Smtp:UseSsl"], out var ssl) && ssl;

            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(fromName, fromEmail));
            mime.To.Add(MailboxAddress.Parse(message.ToAddress));
            mime.Subject = message.Subject ?? string.Empty;

            var bodyBuilder = new BodyBuilder();
            if (message.IsHtml) bodyBuilder.HtmlBody = message.Body;
            else bodyBuilder.TextBody = message.Body;
            mime.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useSsl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable, ct);
            if (!string.IsNullOrEmpty(user)) await client.AuthenticateAsync(user, pass, ct);
            var response = await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);

            return new EmailSendResult(Success: true, ProviderMessageId: response, Error: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Address}", message.ToAddress);
            return new EmailSendResult(Success: false, ProviderMessageId: null, Error: ex.Message);
        }
    }
}

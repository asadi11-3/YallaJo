using Auth.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Auth.Infrastructure.Services;

/// <summary>
/// Gmail SMTP email service using MailKit.
/// <para>
/// Default configuration uses port 465 with implicit TLS
/// (<see cref="SecureSocketOptions.SslOnConnect"/>) instead of port 587 with
/// STARTTLS. Rationale: some AV / ISP / VPN middle-boxes intercept SMTP on
/// 587, complete the TCP handshake locally, then fail to proxy the plaintext
/// banner — causing <c>ConnectAsync</c> to hang until cancellation fires.
/// Implicit TLS starts the TLS handshake with the very first byte the client
/// sends, so a transparent proxy cannot strip or rewrite the banner.
/// </para>
/// <para>
/// All three phases (connect, authenticate, send) are independently bounded
/// by <see cref="GmailOptions.TimeoutSeconds"/> so a stuck middle-box can
/// never hang the outbox processor indefinitely.
/// </para>
/// </summary>
internal sealed class GmailEmailService(
    IOptions<GmailOptions> options,
    ILogger<GmailEmailService> logger) : IEmailService
{
    private readonly GmailOptions _opts = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var senderEmail = NormalizeEmail(_opts.SenderEmail, nameof(GmailOptions.SenderEmail));
        var recipientEmail = NormalizeEmail(to, nameof(to));
        var appPassword = NormalizeAppPassword(_opts.AppPassword);
        var host = string.IsNullOrWhiteSpace(_opts.Host) ? "smtp.gmail.com" : _opts.Host.Trim();
        var port = _opts.Port > 0 ? _opts.Port : 465;
        var secureOpts = ParseSecureOptions(_opts.SecureSocketOptions);
        var timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds > 0 ? _opts.TimeoutSeconds : 15);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(senderEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = (subject ?? string.Empty).Trim();
        message.Body = new TextPart("plain")
        {
            Text = body ?? string.Empty
        };

        using var client = new SmtpClient();
        client.Timeout = (int)timeout.TotalMilliseconds;

        try
        {
            using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                connectCts.CancelAfter(timeout);
                await client.ConnectAsync(host, port, secureOpts, connectCts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError(
                "Gmail SMTP connect timed out after {Timeout}s against {Host}:{Port} ({Mode}). " +
                "TCP likely opens but the SMTP banner/TLS handshake is not completing. " +
                "Probable middle-box interference (AV mail-shield, VPN, transparent proxy). " +
                "If this is port 587, switch Gmail:Port to 465 and Gmail:SecureSocketOptions to SslOnConnect.",
                timeout.TotalSeconds, host, port, secureOpts);
            throw;
        }

        try
        {
            using (var authCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                authCts.CancelAfter(timeout);
                await client.AuthenticateAsync(senderEmail, appPassword, authCts.Token);
            }
        }
        catch (AuthenticationException aex)
        {
            logger.LogError(aex,
                "Gmail SMTP authentication rejected for {Sender}. " +
                "Check Gmail:AppPassword (must be the 16-character app password — spaces are stripped) " +
                "and that 2-Step Verification is enabled on the account.",
                senderEmail);
            throw;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError(
                "Gmail SMTP auth timed out after {Timeout}s against {Host}:{Port}. Pipe opened but AUTH response did not arrive.",
                timeout.TotalSeconds, host, port);
            throw;
        }

        try
        {
            using (var sendCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                sendCts.CancelAfter(timeout);
                await client.SendAsync(message, sendCts.Token);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogError(
                "Gmail SMTP send timed out after {Timeout}s against {Host}:{Port} for recipient {Recipient}.",
                timeout.TotalSeconds, host, port, recipientEmail);
            throw;
        }
        finally
        {
            if (client.IsConnected)
            {
                try { await client.DisconnectAsync(true, CancellationToken.None); }
                catch (Exception ex) { logger.LogDebug(ex, "Gmail SMTP disconnect failed (non-fatal)."); }
            }
        }
    }

    private static SecureSocketOptions ParseSecureOptions(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return SecureSocketOptions.SslOnConnect;
        return value.Trim() switch
        {
            var s when string.Equals(s, "Auto",                       StringComparison.OrdinalIgnoreCase) => SecureSocketOptions.Auto,
            var s when string.Equals(s, "None",                       StringComparison.OrdinalIgnoreCase) => SecureSocketOptions.None,
            var s when string.Equals(s, "SslOnConnect",               StringComparison.OrdinalIgnoreCase) => SecureSocketOptions.SslOnConnect,
            var s when string.Equals(s, "StartTls",                   StringComparison.OrdinalIgnoreCase) => SecureSocketOptions.StartTls,
            var s when string.Equals(s, "StartTlsWhenAvailable",      StringComparison.OrdinalIgnoreCase) => SecureSocketOptions.StartTlsWhenAvailable,
            _ => SecureSocketOptions.SslOnConnect,
        };
    }

    private static string NormalizeAppPassword(string appPassword)
    {
        var normalized = (appPassword ?? string.Empty)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(
                "Gmail AppPassword configuration is missing. " +
                "Set it via User Secrets or the GMAIL__APPPASSWORD environment variable (never commit it).");
        }

        return normalized;
    }

    private static string NormalizeEmail(string email, string parameterName)
    {
        var normalized = (email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException($"Email address '{parameterName}' is missing.");
        }

        return normalized;
    }
}

namespace Auth.Infrastructure.Services;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string SenderEmail { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;

    /// <summary>SMTP host. Override for staging fixtures.</summary>
    public string Host { get; init; } = "smtp.gmail.com";

    /// <summary>
    /// SMTP port. Defaults to 465 (implicit TLS) because port 587 (STARTTLS)
    /// is intercepted by some AV / ISP / VPN middle-boxes that complete the
    /// TCP handshake locally and then fail to proxy the SMTP banner, causing
    /// the client to hang on <c>ConnectAsync</c> until cancellation fires.
    /// Implicit TLS on 465 starts the TLS handshake with the server's first
    /// byte, so a transparent proxy cannot strip or rewrite the banner.
    /// </summary>
    public int Port { get; init; } = 465;

    /// <summary>
    /// One of: "Auto" (MailKit chooses), "SslOnConnect" (port 465),
    /// "StartTls" (port 587), "None" (plaintext — test only).
    /// Defaults to <c>SslOnConnect</c> to match the default port.
    /// </summary>
    public string SecureSocketOptions { get; init; } = "SslOnConnect";

    /// <summary>Client-side total timeout for connect/auth/send in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 15;
}

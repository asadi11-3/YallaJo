namespace Auth.Infrastructure.Services;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string SenderEmail { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;

    public string Host { get; init; } = "smtp.gmail.com";

    public int Port { get; init; } = 465;
    public string SecureSocketOptions { get; init; } = "SslOnConnect";

    public int TimeoutSeconds { get; init; } = 15;
}

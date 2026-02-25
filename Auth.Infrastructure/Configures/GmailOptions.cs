namespace Auth.Infrastructure.Configures;

public sealed class GmailOptions
{
    public const string SectionName = "Gmail";

    public string SenderEmail { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;
}

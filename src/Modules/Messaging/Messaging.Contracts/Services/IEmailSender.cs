namespace Messaging.Contracts.Services;

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string ToAddress,
    string? Subject,
    string Body,
    bool IsHtml = false);

public sealed record EmailSendResult(bool Success, string? ProviderMessageId, string? Error);

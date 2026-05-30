namespace Messaging.Contracts.Services;

public interface ISmsSender
{
    Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken ct = default);
}

public sealed record SmsMessage(
    string ToPhoneNumber,
    string Body);

public sealed record SmsSendResult(bool Success, string? ProviderMessageId, string? Error);

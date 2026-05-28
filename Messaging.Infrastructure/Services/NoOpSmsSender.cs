using Messaging.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Messaging.Infrastructure.Services;

/// <summary>
/// No-op SMS sender used when no real SMS provider is configured.
/// Logs the message and returns success to avoid breaking the notification pipeline.
/// Replace with a real implementation (e.g., Twilio, Vonage) via DI at deployment time.
/// </summary>
internal sealed class NoOpSmsSender(ILogger<NoOpSmsSender> logger) : ISmsSender
{
    public Task<SmsSendResult> SendAsync(SmsMessage message, CancellationToken ct = default)
    {
        logger.LogWarning(
            "SMS not configured. Would have sent to {PhoneNumber}: {Body}",
            message.ToPhoneNumber,
            message.Body);

        return Task.FromResult(new SmsSendResult(
            Success: true,
            ProviderMessageId: null,
            Error: "SMS provider not configured — message logged only"));
    }
}

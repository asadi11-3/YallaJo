using Messaging.Contracts.Services;

namespace Messaging.Infrastructure.Services;

internal sealed class NoopEmailSender : IEmailSender
{
    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
        => Task.FromResult(new EmailSendResult(Success: true, ProviderMessageId: $"noop-{Guid.CreateVersion7():N}", Error: null));
}

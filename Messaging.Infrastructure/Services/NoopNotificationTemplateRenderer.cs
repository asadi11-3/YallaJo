using Messaging.Contracts.Services;

namespace Messaging.Infrastructure.Services;

internal sealed class NoopNotificationTemplateRenderer : INotificationTemplateRenderer
{
    public Task<RenderedTemplate> RenderAsync(string templateCode, IReadOnlyDictionary<string, string> tokens, CancellationToken ct = default)
        => Task.FromResult(new RenderedTemplate(Subject: null, Body: string.Empty));
}

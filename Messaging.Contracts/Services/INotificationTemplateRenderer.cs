namespace Messaging.Contracts.Services;

public interface INotificationTemplateRenderer
{
    Task<RenderedTemplate> RenderAsync(string templateCode, IReadOnlyDictionary<string, string> tokens, CancellationToken ct = default);
}

public sealed record RenderedTemplate(string? Subject, string Body);

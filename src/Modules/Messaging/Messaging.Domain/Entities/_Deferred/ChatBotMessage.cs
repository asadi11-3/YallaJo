using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class ChatBotMessage : BaseEntity
{
    private ChatBotMessage() { } // EF Core

    public Guid ConversationId { get; private set; }
    public bool IsFromBot { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public decimal? Confidence { get; private set; }
    public string? Intent { get; private set; }

    public ChatBotConversation ChatBotConversation { get; private set; } = default!;
}

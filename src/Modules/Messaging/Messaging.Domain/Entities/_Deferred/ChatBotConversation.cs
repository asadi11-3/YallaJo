using YallaJo.SharedKernel.Domain.Entities;

namespace Messaging.Domain.Entities;

public sealed class ChatBotConversation : AuditableEntity, IAggregateRoot
{
    private readonly List<ChatBotMessage> _chatBotMessages = [];

    private ChatBotConversation() { } // EF Core

    public Guid UserId { get; private set; }
    public string? Title { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime? LastMessageAt { get; private set; }

    public IReadOnlyCollection<ChatBotMessage> ChatBotMessages => _chatBotMessages.AsReadOnly();
}

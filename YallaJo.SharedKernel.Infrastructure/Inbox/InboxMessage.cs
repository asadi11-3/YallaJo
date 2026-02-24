namespace YallaJo.SharedKernel.Infrastructure.Inbox
{
    /// <summary>
    /// Records a successfully processed integration event message in a consuming module's schema.
    /// Id = the OutboxMessage.Id from the publishing module, used as the idempotency key.
    /// One row per successfully handled message — primary key prevents double-processing.
    /// </summary>
    public sealed class InboxMessage
    {
        public Guid Id { get; private set; }
        public DateTime ProcessedAt { get; private set; }

        private InboxMessage() { }

        public static InboxMessage Create(Guid messageId) => new InboxMessage
        {
            Id = messageId,
            ProcessedAt = DateTime.UtcNow
        };
    }
}

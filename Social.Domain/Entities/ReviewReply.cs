using YallaJo.SharedKernel.Domain.Entities;

namespace Social.Domain.Entities;

/// <summary>A provider's reply to a user review. Owned by Review aggregate.</summary>
public sealed class ReviewReply : BaseEntity
{
    private ReviewReply() { } // EF Core

    internal ReviewReply(Guid reviewId, Guid providerUserId, string content, DateTime createdAt)
    {
        ReviewId        = reviewId;
        ProviderUserId  = providerUserId;
        Content         = content;
        CreatedAt       = createdAt;
    }

    public Guid ReviewId        { get; private set; }
    public Guid ProviderUserId  { get; private set; }
    public string Content       { get; private set; } = string.Empty;
    public bool IsDeleted       { get; private set; }
    public DateTime CreatedAt   { get; private set; }
    public DateTime? LastEditedAt { get; private set; }

    internal void Update(string newContent, Guid callerUserId)
    {
        if (callerUserId != ProviderUserId)
            throw new UnauthorizedAccessException("Only the reply author can edit their reply.");
        Content = newContent;
        LastEditedAt = DateTime.UtcNow;
    }

    internal void Delete(Guid callerUserId) => IsDeleted = true;
}

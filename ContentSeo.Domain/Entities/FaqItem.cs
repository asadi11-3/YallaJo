using ContentSeo.Domain.Enums;
using ContentSeo.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class FaqItem : AuditableEntity, IAggregateRoot
{
    private const int MaxQuestionLength = 1000;
    private const int MaxAnswerLength   = 1000;

    private readonly List<FaqItemTranslation> _translations = [];

    private FaqItem() { } // EF Core

    public SeoEntityType EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<FaqItemTranslation> FaqItemTranslations => _translations.AsReadOnly();

    // ── Factory ───────────────────────────────────────────────────────────────

    public static FaqItem Create(
        SeoEntityType entityType,
        Guid entityId,
        string question,
        string answer,
        int sortOrder = 0)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("EntityId cannot be empty.", nameof(entityId));

        ValidateQuestion(question);
        ValidateAnswer(answer);
        ValidateSortOrder(sortOrder);

        var item = new FaqItem
        {
            Id         = Guid.CreateVersion7(),
            EntityType = entityType,
            EntityId   = entityId,
            Question   = question.Trim(),
            Answer     = answer.Trim(),
            SortOrder  = sortOrder,
            IsActive   = true,
        };

        item.AddDomainEvent(new FaqItemCreatedDomainEvent(item.Id, item.EntityType, item.EntityId));

        return item;
    }

    // ── Business Methods ──────────────────────────────────────────────────────

    public void Update(string question, string answer)
    {
        EnsureNotDeleted();
        ValidateQuestion(question);
        ValidateAnswer(answer);

        Question = question.Trim();
        Answer   = answer.Trim();
        MarkUpdated();

        AddDomainEvent(new FaqItemUpdatedDomainEvent(Id, EntityType, EntityId));
    }

    public void Reorder(int newSortOrder)
    {
        EnsureNotDeleted();
        ValidateSortOrder(newSortOrder);

        var oldSortOrder = SortOrder;  // capture BEFORE mutation
        SortOrder = newSortOrder;
        MarkUpdated();

        AddDomainEvent(new FaqItemReorderedDomainEvent(
            Id, EntityType, EntityId,
            OldSortOrder: oldSortOrder,
            NewSortOrder: newSortOrder));
    }

    public void Activate()
    {
        EnsureNotDeleted();
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        IsActive = false;
        MarkUpdated();
    }

    public new void SoftDelete()
    {
        if (IsDeleted) return;

        base.SoftDelete();

        AddDomainEvent(new FaqItemDeletedDomainEvent(Id, EntityType, EntityId));
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "FaqItem.Deleted: operation not permitted on a soft-deleted FAQ item.");
    }

    // ── Validators ────────────────────────────────────────────────────────────

    private static void ValidateQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));
        if (question.Trim().Length > MaxQuestionLength)
            throw new ArgumentException(
                $"Question cannot exceed {MaxQuestionLength} characters.", nameof(question));
    }

    private static void ValidateAnswer(string answer)
    {
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException("Answer is required.", nameof(answer));
        if (answer.Trim().Length > MaxAnswerLength)
            throw new ArgumentException(
                $"Answer cannot exceed {MaxAnswerLength} characters.", nameof(answer));
    }

    private static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder), "SortOrder cannot be negative.");
    }
}

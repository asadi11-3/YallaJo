using YallaJo.SharedKernel.Domain.Entities;

namespace ContentSeo.Domain.Entities;

public sealed class FaqItemTranslation : BaseEntity
{
    private FaqItemTranslation() { } // EF Core

    public Guid FaqItemId { get; private set; }
    public Guid LanguageId { get; private set; }
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;

    public FaqItem FaqItem { get; private set; } = default!;

    public static FaqItemTranslation Create(
        Guid faqItemId,
        Guid languageId,
        string question,
        string answer)
    {
        if (faqItemId == Guid.Empty)
            throw new ArgumentException("FaqItem is required.", nameof(faqItemId));
        if (languageId == Guid.Empty)
            throw new ArgumentException("Language is required.", nameof(languageId));
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Translation question is required.", nameof(question));
        if (string.IsNullOrWhiteSpace(answer))
            throw new ArgumentException("Translation answer is required.", nameof(answer));

        return new FaqItemTranslation
        {
            FaqItemId = faqItemId,
            LanguageId = languageId,
            Question = question.Trim(),
            Answer = answer.Trim()
        };
    }
}

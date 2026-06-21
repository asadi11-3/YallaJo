namespace ContentCore.Contracts.Authorization;

/// <summary>
/// Feature string constants owned by the ContentCore bounded context.
/// </summary>
public static class ContentCoreFeatures
{
    public const string Category            = nameof(Category);
    public const string CategoryTranslation = nameof(CategoryTranslation);
    public const string Specialization      = nameof(Specialization);
    public const string Tag                 = nameof(Tag);
    public const string EntityCategory      = nameof(EntityCategory);
    public const string EntityImage         = nameof(EntityImage);
    public const string EntityTag           = nameof(EntityTag);
    public const string TranslationCache    = nameof(TranslationCache);
    public const string Language            = nameof(Language);
    public const string Attachment          = nameof(Attachment);
    public const string Promotion           = nameof(Promotion);
}

namespace ContentCore.Domain.Enums;

public enum TranslationStatus : byte
{
    Pending = 0,
    AutoTranslated = 1,
    HumanReviewed = 2
}

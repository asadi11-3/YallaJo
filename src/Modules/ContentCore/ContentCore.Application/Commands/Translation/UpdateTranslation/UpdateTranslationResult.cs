namespace ContentCore.Application.Commands.Translation.UpdateTranslation;

public sealed record UpdateTranslationResult(Guid Id, string TranslatedText, string Status);

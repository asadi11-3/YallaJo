namespace ContentCore.Presentation.Endpoints.Translation.Models;

public sealed record BatchApproveTranslationsRequest(
    string EntityType,
    Guid EntityId,
    string LanguageCode,
    IReadOnlyList<string>? FieldNames = null);

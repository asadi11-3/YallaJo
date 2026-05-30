using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.BatchApproveTranslations;

/// <summary>
/// Marks all auto-translated fields as human-reviewed in one shot for a given
/// entity + language combination. Optionally restrict to specific field names.
/// </summary>
public sealed record BatchApproveTranslationsCommand(
    string EntityType,
    Guid EntityId,
    string LanguageCode,
    IReadOnlyList<string>? FieldNames = null) : ICommand<BatchApproveTranslationsResult>;

namespace ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;

public sealed record TriggerTranslationBackfillResult(
    string EntityKind,
    int TotalProcessed,
    int TotalTranslationsAdded,
    int TotalSkipped);

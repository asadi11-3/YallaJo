using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentCore.Application.Commands.Translation.TriggerTranslationBackfill;

/// <summary>
/// Triggers a one-time translation backfill for all existing Tag or Specialization rows
/// that have zero translations. Safe to call multiple times — skips rows that already
/// have translations for a given language.
/// </summary>
/// <param name="EntityKind">"tag" or "specialization" (case-insensitive)</param>
public sealed record TriggerTranslationBackfillCommand(
    string EntityKind) : ICommand<TriggerTranslationBackfillResult>;

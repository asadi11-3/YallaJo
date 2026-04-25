using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Domain.Events;

/// <summary>
/// Raised by <see cref="ContentCore.Domain.Entities.Language.Deactivate"/> when a language
/// transitions from active to inactive. Guarded by the entity — repeat calls are no-ops.
///
/// <para>The corresponding infrastructure handler writes a
/// <see cref="ContentCore.Contracts.IntegrationEvents.LanguageDeactivatedIntegrationEvent"/>
/// to the outbox so downstream modules can react (e.g. stop auto-translating into this language).</para>
/// </summary>
public sealed record LanguageDeactivatedDomainEvent(
    Guid LanguageId,
    string LanguageCode) : DomainEventBase;

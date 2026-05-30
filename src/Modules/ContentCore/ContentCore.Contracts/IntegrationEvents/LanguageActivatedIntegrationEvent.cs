using YallaJo.SharedKernel.Domain.Event;

namespace ContentCore.Contracts.IntegrationEvents;

public sealed record LanguageActivatedIntegrationEvent(
    Guid LanguageId,
    string LanguageCode) : IntegrationEventBase;

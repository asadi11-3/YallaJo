using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Domain.Event;

namespace Security.Contracts.IntegrationEvents;

public sealed record UserLifecycleChangedIntegrationEvent(
    Guid UserId,
    AccountLifecycleSnapshot From,
    AccountLifecycleSnapshot To) : IntegrationEventBase;

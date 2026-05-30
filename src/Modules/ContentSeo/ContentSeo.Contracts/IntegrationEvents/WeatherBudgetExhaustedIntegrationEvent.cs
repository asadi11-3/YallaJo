namespace ContentSeo.Contracts.IntegrationEvents;

using YallaJo.SharedKernel.Domain.Event;

public sealed record WeatherBudgetExhaustedIntegrationEvent(
    DateOnly Date,
    int CallsUsed,
    int DailyLimit,
    DateTime ExhaustedAt) : IntegrationEventBase;

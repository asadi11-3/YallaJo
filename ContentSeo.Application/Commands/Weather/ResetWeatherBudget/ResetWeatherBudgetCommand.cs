using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentSeo.Application.Commands.Weather.ResetWeatherBudget;

public sealed record ResetWeatherBudgetCommand(DateOnly? Date) : ICommand;

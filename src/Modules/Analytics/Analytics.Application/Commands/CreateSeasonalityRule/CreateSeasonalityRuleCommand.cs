using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.CreateSeasonalityRule;

public sealed record CreateSeasonalityRuleCommand(
    Guid PlaceId,
    int MonthStart,
    int MonthEnd,
    decimal Multiplier,
    string? Description) : ICommand<CreateSeasonalityRuleResult>;

public sealed record CreateSeasonalityRuleResult(Guid Id);

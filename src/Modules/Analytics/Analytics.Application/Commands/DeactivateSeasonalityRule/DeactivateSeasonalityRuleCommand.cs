using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Commands.DeactivateSeasonalityRule;

public sealed record DeactivateSeasonalityRuleCommand(Guid RuleId) : ICommand;

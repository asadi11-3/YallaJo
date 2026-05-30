using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.DeleteCommissionRule;

public sealed record DeleteCommissionRuleCommand(Guid RuleId, string? Reason) : IRequest<Result<bool>>;

using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.UpdateCommissionRule;

public sealed record UpdateCommissionRuleCommand(
    Guid RuleId,
    decimal MinMonthlyRevenue,
    decimal? MaxMonthlyRevenue,
    decimal Percentage,
    string? Notes) : IRequest<Result<CommissionRuleDto>>;

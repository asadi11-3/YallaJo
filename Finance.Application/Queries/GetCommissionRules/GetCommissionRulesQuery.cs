using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Queries.GetCommissionRules;

public sealed record GetCommissionRulesQuery(
    string? Tier,
    string? Currency,
    bool IncludeInactive) : IRequest<Result<IReadOnlyList<CommissionRuleDto>>>;

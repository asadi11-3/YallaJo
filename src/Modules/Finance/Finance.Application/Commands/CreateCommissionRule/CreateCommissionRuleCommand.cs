using Finance.Application.Queries.Dtos;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.CreateCommissionRule;

public sealed record CreateCommissionRuleCommand(
    string Tier,
    decimal MinMonthlyRevenue,
    decimal? MaxMonthlyRevenue,
    string Currency,
    decimal Percentage,
    string? Notes) : IRequest<Result<CommissionRuleDto>>;

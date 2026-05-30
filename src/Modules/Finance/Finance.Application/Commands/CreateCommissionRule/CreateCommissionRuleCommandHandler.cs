using Finance.Application.Interfaces;
using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetCommissionRules;
using Finance.Domain.Entities;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.CreateCommissionRule;

public sealed class CreateCommissionRuleCommandHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CreateCommissionRuleCommand, Result<CommissionRuleDto>>
{
    public async Task<Result<CommissionRuleDto>> Handle(CreateCommissionRuleCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // F-R6 overlap detection
        var overlapping = await commissionRuleRepository.GetOverlappingAsync(
            request.Tier, request.Currency, request.MinMonthlyRevenue, request.MaxMonthlyRevenue, ct);
        if (overlapping.Count > 0)
        {
            return Result.Failure<CommissionRuleDto>(
                new Error("CommissionRule.OverlapTier", "Another active commission rule overlaps with the requested (Tier, Currency, range)."),
                Outcome.Conflict);
        }

        CommissionRule rule;
        try
        {
            rule = CommissionRule.Create(
                request.Tier, request.MinMonthlyRevenue, request.MaxMonthlyRevenue,
                request.Currency, request.Percentage, request.Notes, timeProvider);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CommissionRuleDto>(
                new Error("CommissionRule.Invalid", ex.Message),
                Outcome.Invalid);
        }

        await commissionRuleRepository.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(GetCommissionRulesQueryHandler.MapToDto(rule));
    }
}

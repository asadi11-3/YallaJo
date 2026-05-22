using Finance.Application.Interfaces;
using Finance.Application.Queries.Dtos;
using Finance.Application.Queries.GetCommissionRules;
using Finance.Domain.Repositories;
using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.UpdateCommissionRule;

public sealed class UpdateCommissionRuleCommandHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IFinanceUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCommissionRuleCommand, Result<CommissionRuleDto>>
{
    public async Task<Result<CommissionRuleDto>> Handle(UpdateCommissionRuleCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rule = await commissionRuleRepository.GetByIdAsync(request.RuleId, ct);
        if (rule is null)
        {
            return Result.Failure<CommissionRuleDto>(
                new Error("CommissionRule.NotFound", $"Rule {request.RuleId} not found."),
                Outcome.NotFound);
        }

        // Overlap check (excluding self)
        var overlapping = await commissionRuleRepository.GetOverlappingAsync(
            rule.Tier, rule.Currency, request.MinMonthlyRevenue, request.MaxMonthlyRevenue, ct);
        if (overlapping.Any(r => r.Id != rule.Id))
        {
            return Result.Failure<CommissionRuleDto>(
                new Error("CommissionRule.OverlapTier", "Another active commission rule overlaps with the requested range."),
                Outcome.Conflict);
        }

        try
        {
            rule.Update(request.MinMonthlyRevenue, request.MaxMonthlyRevenue, request.Percentage, request.Notes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<CommissionRuleDto>(
                new Error("CommissionRule.Invalid", ex.Message),
                Outcome.Invalid);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(GetCommissionRulesQueryHandler.MapToDto(rule));
    }
}

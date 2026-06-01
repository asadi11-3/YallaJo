using Finance.Application.Interfaces;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.DeleteCommissionRule;

public sealed class DeleteCommissionRuleCommandHandler(
    ICommissionRuleRepository commissionRuleRepository,
    IFinanceUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCommissionRuleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteCommissionRuleCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var rule = await commissionRuleRepository.GetByIdAsync(request.RuleId, ct);
        if (rule is null)
        {
            return Result.Failure<bool>(
                new Error("CommissionRule.NotFound", $"Rule {request.RuleId} not found."),
                Outcome.NotFound);
        }

        rule.Deactivate(request.Reason);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<bool>(
                new Error("CommissionRule.ConcurrencyConflict", "The commission rule was modified concurrently. Reload and retry."),
                Outcome.Conflict);
        }

        return Result.Success(true);
    }
}

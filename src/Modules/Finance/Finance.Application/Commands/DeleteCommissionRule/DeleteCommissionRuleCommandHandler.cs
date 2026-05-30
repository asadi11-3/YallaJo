using Finance.Application.Interfaces;
using Finance.Domain.Repositories;
using MediatR;
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
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

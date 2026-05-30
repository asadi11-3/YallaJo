using Finance.Application.Interfaces;
using Finance.Contracts.Services;
using Finance.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Finance.Application.Commands.ApprovePayout;

public sealed class ApprovePayoutCommandHandler(
    IPayoutRepository payoutRepository,
    IPaymentGateway gateway,
    IFinanceUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<ApprovePayoutCommandHandler> logger)
    : IRequestHandler<ApprovePayoutCommand, Result<ApprovePayoutResult>>
{
    public async Task<Result<ApprovePayoutResult>> Handle(ApprovePayoutCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payout = await payoutRepository.GetByIdAsync(request.PayoutId, ct);
        if (payout is null)
        {
            return Result.Failure<ApprovePayoutResult>(
                new Error("Payout.NotFound", $"Payout {request.PayoutId} not found."),
                Outcome.NotFound);
        }

        var approveResult = payout.Approve(request.ApproverUserId, timeProvider);
        if (approveResult.IsFailure)
        {
            return Result.Failure<ApprovePayoutResult>(
                approveResult.Errors.FirstOrDefault() ?? new Error("Payout.InvalidState", "Payout could not be approved."),
                approveResult.Outcome);
        }

        // Gateway call
        try
        {
            var payoutResult = await gateway.PayoutAsync(
                new PayoutRequest(
                    payout.Id,
                    payout.BankAccountId?.ToString() ?? "default",
                    payout.NetAmount.Amount,
                    payout.Currency),
                ct);

            switch (payoutResult.Status)
            {
                case PayoutGatewayStatus.Completed:
                    var completed = payout.MarkCompleted(payoutResult.GatewayPayoutId, timeProvider.GetUtcNow().UtcDateTime);
                    if (completed.IsFailure)
                    {
                        return Result.Failure<ApprovePayoutResult>(completed.Errors.First(), completed.Outcome);
                    }
                    break;
                case PayoutGatewayStatus.Pending:
                    // Leave as ReadyForPayout; webhook will finalise (deferred to Phase 3).
                    break;
                case PayoutGatewayStatus.Failed:
                    var failed = payout.MarkFailed(
                        payoutResult.FailureCode ?? "Gateway.Failed",
                        timeProvider.GetUtcNow().UtcDateTime);
                    if (failed.IsFailure)
                    {
                        return Result.Failure<ApprovePayoutResult>(failed.Errors.First(), failed.Outcome);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gateway PayoutAsync threw for payout {PayoutId}.", payout.Id);
            payout.MarkFailed("Gateway.Exception", timeProvider.GetUtcNow().UtcDateTime);
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new ApprovePayoutResult(payout.Id, payout.Status.ToString(), payout.GatewayPayoutId));
    }
}

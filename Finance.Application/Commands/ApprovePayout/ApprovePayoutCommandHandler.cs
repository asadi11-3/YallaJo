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

        try
        {
            payout.Approve(request.ApproverUserId, timeProvider);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ApprovePayoutResult>(
                new Error("Payout.AlreadyApproved", ex.Message),
                Outcome.Conflict);
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
                    payout.MarkCompleted(payoutResult.GatewayPayoutId, timeProvider.GetUtcNow().UtcDateTime);
                    break;
                case PayoutGatewayStatus.Pending:
                    // Leave as ReadyForPayout; webhook will finalise (deferred to Phase 3).
                    break;
                case PayoutGatewayStatus.Failed:
                    payout.MarkFailed(
                        payoutResult.FailureCode ?? "Gateway.Failed",
                        timeProvider.GetUtcNow().UtcDateTime);
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

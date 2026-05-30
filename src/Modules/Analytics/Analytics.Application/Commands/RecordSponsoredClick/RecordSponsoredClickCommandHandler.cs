using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.RecordSponsoredClick;

public sealed class RecordSponsoredClickCommandHandler(
    IBoostPackageRepository boostRepo,
    ISponsoredClickEventRepository clickRepo,
    IAnalyticsUnitOfWork unitOfWork) : ICommandHandler<RecordSponsoredClickCommand>
{
    public async Task<Result> Handle(RecordSponsoredClickCommand request, CancellationToken ct)
    {
        // Anti-fraud: dwell time < 2s = no charge
        if (request.DwellTimeSeconds < 2)
            return Result.Success();

        // Anti-fraud: 1 charge per (user/session, bid) per day
        var alreadyCharged = await clickRepo.HasChargedTodayAsync(
            request.BidId, request.UserId, request.SessionId, ct);
        if (alreadyCharged)
            return Result.Success();

        var bid = await boostRepo.GetByIdAsync(request.BidId, ct);
        if (bid is null || !bid.IsActive || bid.BillingMode != "CPC")
            return Result.Success();

        // Check daily budget
        if (!bid.HasBudgetRemaining())
            return Result.Success();

        var chargeAmount = bid.BidPerClick ?? 0m;
        bid.ChargeClick(chargeAmount);

        var clickEvent = SponsoredClickEvent.Record(
            request.BidId, request.UserId, request.SessionId,
            request.SourceKind, request.SourceId, request.Position,
            chargeAmount, DateTime.UtcNow);

        await clickRepo.AddAsync(clickEvent, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

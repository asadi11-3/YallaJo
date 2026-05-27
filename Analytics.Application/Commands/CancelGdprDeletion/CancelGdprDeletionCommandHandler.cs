using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.CancelGdprDeletion;

public sealed class CancelGdprDeletionCommandHandler(
    IGdprDeletionRequestRepository gdprRepo,
    IAnalyticsUnitOfWork unitOfWork) : ICommandHandler<CancelGdprDeletionCommand>
{
    public async Task<Result> Handle(CancelGdprDeletionCommand request, CancellationToken ct)
    {
        var pending = await gdprRepo.GetPendingByUserAsync(request.UserId, ct);
        if (pending is null)
            return Result.Failure(new Error("Gdpr.NotFound", "No pending deletion request found."), Outcome.NotFound);

        pending.Cancel();
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

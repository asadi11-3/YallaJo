using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.RequestGdprDeletion;

public sealed record RequestGdprDeletionCommand(Guid UserId) : ICommand;

public sealed class RequestGdprDeletionCommandHandler(
    IGdprDeletionRequestRepository gdprRepo,
    IAnalyticsUnitOfWork unitOfWork) : ICommandHandler<RequestGdprDeletionCommand>
{
    public async Task<Result> Handle(RequestGdprDeletionCommand request, CancellationToken ct)
    {
        // Check if there's already a pending request
        var existing = await gdprRepo.GetPendingByUserAsync(request.UserId, ct);
        if (existing is not null)
            return Result.Failure(new Error("Gdpr.AlreadyPending", "A deletion request is already pending."));

        var deletionRequest = GdprDeletionRequest.Create(request.UserId);
        await gdprRepo.AddAsync(deletionRequest, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}

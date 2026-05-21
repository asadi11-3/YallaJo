using MediatR;
using Messaging.Application.Interfaces;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.DeleteDeviceToken;

internal sealed class DeleteDeviceTokenCommandHandler(
    IDeviceTokenRepository deviceTokenRepository,
    IMessagingUnitOfWork unitOfWork) : IRequestHandler<DeleteDeviceTokenCommand, Result>
{
    public async Task<Result> Handle(DeleteDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await deviceTokenRepository.GetByIdAsync(request.TokenId, cancellationToken);
        if (token is null)
            return Result.Failure(new Error("DeviceToken.NotFound", "Device token not found."), Outcome.NotFound);

        if (token.UserId != request.CallerUserId)
            return Result.Failure(new Error("DeviceToken.OwnerMismatch", "You do not own this device token."), Outcome.Forbidden);

        token.Delete();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

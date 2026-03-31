using Security.Application.Interfaces;
using Security.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Security.Application.Commands.UpdatePhone;

public sealed class UpdatePrimaryPhoneCommandHandler(
    IUserRepository userRepository,
    ISecurityUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<UpdatePrimaryPhoneCommand, UpdatePrimaryPhoneResult>
{
    public async Task<Result<UpdatePrimaryPhoneResult>> Handle(
        UpdatePrimaryPhoneCommand request,
        CancellationToken ct)
    {
        if (currentUser.UserId is null) {
            return Result<UpdatePrimaryPhoneResult>.Failure(
                 Error.Unauthorized("Authentication is required."),
                 Outcome.Unauthorized);
        }

        var user = await userRepository.GetByIdWithPhonesAsync(currentUser.UserId.Value, ct);
        if (user is null) {
            return Result<UpdatePrimaryPhoneResult>.Failure(
                   Error.NotFound("User", "User not found."),
                   Outcome.NotFound);
        }

        var phone = user.UpdatePrimaryPhone(request.PhoneNumber);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<UpdatePrimaryPhoneResult>.Success(
            new UpdatePrimaryPhoneResult(true, phone.PhoneNumber));
    }
}

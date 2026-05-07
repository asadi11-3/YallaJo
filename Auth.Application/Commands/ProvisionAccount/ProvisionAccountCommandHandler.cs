using Accounts.Contracts.Abstractions;
using Security.Contracts.Abstractions;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Auth.Application.Commands.ProvisionAccount;

public sealed class ProvisionAccountCommandHandler(
    IUserRegistrationService userRegistrationService,
    IProfileCreationService profileCreationService)
    : ICommandHandler<ProvisionAccountCommand, ProvisionAccountResult>
{
    public async Task<Result<ProvisionAccountResult>> Handle(
        ProvisionAccountCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var registration = await userRegistrationService.RegisterProvisionedAsync(
            new InvitedUserRegistrationRequest(
                request.FirstName,
                request.LastName,
                normalizedEmail,
                request.InitialRoleIds),
            cancellationToken);

        if (registration.IsFailure)
        {
            return Result<ProvisionAccountResult>.Fail(
                registration.Outcome,
                registration.Messages.FirstOrDefault() ?? string.Empty,
                registration.Errors.ToArray());
        }

        var userId = registration.Value;

        var profile = await profileCreationService.CreateForInvitedUserAsync(
            new InvitedProfileCreationRequest(
                UserId:      userId,
                FirstName:   request.FirstName,
                LastName:    request.LastName,
                DisplayName: string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
                AvatarUrl:   string.IsNullOrWhiteSpace(request.AvatarUrl)   ? null : request.AvatarUrl.Trim()),
            cancellationToken);

        if (profile.IsFailure && profile.Outcome != Outcome.Conflict)
        {
            return Result<ProvisionAccountResult>.Fail(
                profile.Outcome,
                profile.Messages.FirstOrDefault() ?? string.Empty,
                profile.Errors.ToArray());
        }

        return Result<ProvisionAccountResult>.Created(
            new ProvisionAccountResult(userId, profile.Value));
    }
}

using Accounts.Domain.Errors;
using Accounts.Domain.Repositories;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Application.Queries.Dashboard;

public sealed class GetProviderSettingsQueryHandler(
    IProviderApplicationRepository providerApplicationRepository,
    ICurrentUser currentUser)
    : IQueryHandler<GetProviderSettingsQuery, ProviderSettingsResult>
{
    public async Task<Result<ProviderSettingsResult>> Handle(
        GetProviderSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var application = await providerApplicationRepository.GetByUserIdAsync(userId, cancellationToken);
        if (application is null)
            return Result<ProviderSettingsResult>.Failure(
                ProviderApplicationErrors.NotFound, Outcome.NotFound);

        return Result<ProviderSettingsResult>.Success(new ProviderSettingsResult(
            BusinessName: application.BusinessName,
            ContactEmail: application.ContactEmail,
            ContactPhone: application.ContactPhone,
            Address: application.Address,
            Description: application.Description,
            ProviderType: application.Type,
            TypeSpecificDataJson: application.TypeSpecificDataJson));
    }
}

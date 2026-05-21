using MediatR;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyDeviceTokens;

internal sealed class GetMyDeviceTokensQueryHandler(
    IDeviceTokenRepository deviceTokenRepository) : IRequestHandler<GetMyDeviceTokensQuery, Result<IReadOnlyList<DeviceTokenDto>>>
{
    public async Task<Result<IReadOnlyList<DeviceTokenDto>>> Handle(GetMyDeviceTokensQuery request, CancellationToken cancellationToken)
    {
        var tokens = await deviceTokenRepository.GetActiveByUserAsync(request.UserId, cancellationToken);
        // NEVER return the actual token value (M-R9)
        var dtos = tokens.Select(t => new DeviceTokenDto(t.Id, t.DeviceId, t.Platform.ToString(), t.LastSeenAt, t.CreatedAt)).ToList();
        return Result.Success<IReadOnlyList<DeviceTokenDto>>(dtos);
    }
}

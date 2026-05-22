using MediatR;
using Messaging.Domain.Repositories;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Queries.GetMyPreferences;

internal sealed class GetMyPreferencesQueryHandler(
    INotificationPreferenceRepository preferenceRepository) : IRequestHandler<GetMyPreferencesQuery, Result<IReadOnlyList<NotificationPreferenceDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(GetMyPreferencesQuery request, CancellationToken cancellationToken)
    {
        var prefs = await preferenceRepository.GetByUserAsync(request.UserId, cancellationToken);
        var dtos = prefs.Select(p => new NotificationPreferenceDto(p.NotificationType.ToString(), p.Channel.ToString(), p.IsEnabled))
                        .ToList();
        return Result.Success<IReadOnlyList<NotificationPreferenceDto>>(dtos);
    }
}

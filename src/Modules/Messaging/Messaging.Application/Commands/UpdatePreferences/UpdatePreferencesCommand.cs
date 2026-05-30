using MediatR;
using Messaging.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Messaging.Application.Commands.UpdatePreferences;

public sealed record PreferenceUpdate(NotificationType Type, NotificationChannel Channel, bool IsEnabled);

public sealed record UpdatePreferencesCommand(
    Guid UserId,
    IReadOnlyList<PreferenceUpdate> Updates) : IRequest<Result>;

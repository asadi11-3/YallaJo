using Messaging.Application.Commands.UpdatePreferences;

namespace Messaging.Presentation.Endpoints.Notification.Models;

internal sealed record UpdatePreferencesRequest(IReadOnlyList<PreferenceUpdate> Updates);

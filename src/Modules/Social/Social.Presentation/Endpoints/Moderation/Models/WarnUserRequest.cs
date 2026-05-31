using Social.Application.Commands.WarnUser;
using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Moderation.Models;

internal sealed record WarnUserRequest(
    Guid UserId,
    ReportableEntityType EntityType,
    Guid EntityId,
    string Reason)
{
    public WarnUserCommand ToCommand(Guid adminUserId) =>
        new(UserId, EntityType, EntityId, Reason, adminUserId);
}

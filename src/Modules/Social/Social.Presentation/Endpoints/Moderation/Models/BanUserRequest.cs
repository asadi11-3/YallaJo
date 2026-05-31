using Social.Application.Commands.BanUser;
using Social.Domain.Enums;

namespace Social.Presentation.Endpoints.Moderation.Models;

internal sealed record BanUserRequest(
    Guid UserId,
    ReportableEntityType EntityType,
    Guid EntityId,
    string Reason,
    DateTime? ExpiresAt)
{
    public BanUserCommand ToCommand(Guid adminUserId) =>
        new(UserId, EntityType, EntityId, Reason, ExpiresAt, adminUserId);
}

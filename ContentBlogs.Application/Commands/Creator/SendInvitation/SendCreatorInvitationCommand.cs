using ContentBlogs.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.SendInvitation;

public sealed record SendCreatorInvitationCommand(
    CreatorInvitationKind Kind,
    string? Email,
    Guid? InvitedUserId,
    string? PersonalMessage) : ICommand<SendCreatorInvitationResult>;

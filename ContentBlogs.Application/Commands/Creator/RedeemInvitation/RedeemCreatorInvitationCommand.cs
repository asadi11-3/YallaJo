using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.RedeemInvitation;

public sealed record RedeemCreatorInvitationCommand(string Token) : ICommand;

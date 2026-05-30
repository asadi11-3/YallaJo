using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.RemoveHelpfulVote;

public sealed record RemoveHelpfulVoteCommand(Guid ReviewId, Guid UserId) : ICommand;

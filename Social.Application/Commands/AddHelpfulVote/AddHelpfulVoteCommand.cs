using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Social.Application.Commands.AddHelpfulVote;

public sealed record AddHelpfulVoteCommand(Guid ReviewId, Guid UserId) : ICommand;

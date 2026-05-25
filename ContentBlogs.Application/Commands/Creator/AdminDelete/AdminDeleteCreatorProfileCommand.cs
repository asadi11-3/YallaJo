using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.AdminDelete;

public sealed record AdminDeleteCreatorProfileCommand(Guid ProfileId, string Reason) : ICommand;

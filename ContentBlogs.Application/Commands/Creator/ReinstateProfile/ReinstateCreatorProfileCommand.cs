using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.ReinstateProfile;

public sealed record ReinstateCreatorProfileCommand(Guid ProfileId) : ICommand;

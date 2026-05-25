using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.UpdateCoverImage;

public sealed record UpdateCreatorCoverImageCommand(string CoverImageUrl) : ICommand;

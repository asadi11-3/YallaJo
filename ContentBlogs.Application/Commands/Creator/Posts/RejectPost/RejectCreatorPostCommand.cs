using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Creator.Posts.RejectPost;

public sealed record RejectCreatorPostCommand(Guid PostId, string Reason) : ICommand;

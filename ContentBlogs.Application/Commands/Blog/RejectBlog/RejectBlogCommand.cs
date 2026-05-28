using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace ContentBlogs.Application.Commands.Blog.RejectBlog;

public sealed record RejectBlogCommand(Guid BlogId, byte[] RowVersion, string Reason) : ICommand;

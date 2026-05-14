using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Application.Commands.Blog.CreateBlog;

public sealed class CreateBlogCommandHandler
    : ICommandHandler<CreateBlogCommand, CreateBlogResult>
{
    public Task<Result<CreateBlogResult>> Handle(
        CreateBlogCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CreateBlogResult>(
            new Error("Blog.NotImplemented", "Blog creation is not implemented yet."),
            Outcome.Invalid));
    }
}

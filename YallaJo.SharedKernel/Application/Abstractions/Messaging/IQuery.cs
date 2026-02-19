using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
}

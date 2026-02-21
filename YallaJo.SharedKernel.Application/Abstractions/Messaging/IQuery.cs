using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
}

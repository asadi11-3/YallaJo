using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    public interface ICommand : IRequest<Result>;
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
}

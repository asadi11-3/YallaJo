using MediatR;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    public interface ICommand : IRequest<Result>;
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
}

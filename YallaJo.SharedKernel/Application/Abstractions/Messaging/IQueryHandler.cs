using MediatR;
using YallaJo.SharedKernel.Application.Abstractions.Results;

namespace YallaJo.SharedKernel.Application.Abstractions.Messaging
{
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>;
}

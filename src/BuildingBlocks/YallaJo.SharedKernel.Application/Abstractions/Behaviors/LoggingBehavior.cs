using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors
{
    public sealed class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogDebug("Starting {RequestName}", requestName);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                stopwatch.Stop();
                logger.LogDebug("Completed {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Failed {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

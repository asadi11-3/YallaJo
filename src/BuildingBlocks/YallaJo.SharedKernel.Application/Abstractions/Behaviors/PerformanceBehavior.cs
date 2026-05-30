using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors
{
    public sealed class PerformanceBehavior<TRequest, TResponse>(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private const int SlowRequestThresholdMs = 500;

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var response = await next();
                stopwatch.Stop();
                LogIfSlow(stopwatch.ElapsedMilliseconds);
                return response;
            }
            catch
            {
                stopwatch.Stop();
                LogIfSlow(stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void LogIfSlow(long elapsedMs)
        {
            if (elapsedMs > SlowRequestThresholdMs)
                logger.LogWarning(
                    "Slow request detected: {RequestName} took {ElapsedMs}ms (threshold: {ThresholdMs}ms)",
                    typeof(TRequest).Name, elapsedMs, SlowRequestThresholdMs);
        }
    }
}

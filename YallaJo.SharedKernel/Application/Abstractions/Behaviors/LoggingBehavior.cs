using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Behaviors
{
  
    public class LoggingBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = Guid.NewGuid();

            // ═══════════════════════════════════════
            // قبل التنفيذ
            // ═══════════════════════════════════════

            _logger.LogInformation(
                "[{RequestId}] Starting {RequestName}",
                requestId,
                requestName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // ═══════════════════════════════════════
                // نفذ Handler
                // ═══════════════════════════════════════

                var response = await next();

                stopwatch.Stop();

                // ═══════════════════════════════════════
                // بعد التنفيذ الناجح
                // ═══════════════════════════════════════

                _logger.LogInformation(
                    "[{RequestId}] Completed {RequestName} in {ElapsedMs}ms",
                    requestId,
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // ═══════════════════════════════════════
                // بعد الفشل
                // ═══════════════════════════════════════

                _logger.LogError(
                    ex,
                    "[{RequestId}] Failed {RequestName} in {ElapsedMs}ms",
                    requestId,
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                throw;
            }
        }
    }
}

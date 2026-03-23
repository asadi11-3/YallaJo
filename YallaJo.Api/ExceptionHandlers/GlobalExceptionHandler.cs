using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace YallaJo.Api.ExceptionHandlers;

/// <summary>
/// Catch-all exception handler. Runs AFTER <see cref="ValidationExceptionHandler"/>
/// and <see cref="DbUpdateExceptionHandler"/>. Converts any unhandled exception into
/// a clean RFC 7807 ProblemDetails with a correlation ID for log correlation.
/// Never leaks stack traces, SQL errors, or internal details to the client.
/// </summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = Activity.Current?.Id
            ?? httpContext.TraceIdentifier;

        // ── Log with full context ─────────────────────────────────────────
        logger.LogError(
            exception,
            "Unhandled exception | CorrelationId={CorrelationId} | Path={Path} | Method={Method}",
            correlationId,
            httpContext.Request.Path.Value,
            httpContext.Request.Method);

        // ── Map known exception types to specific status codes ────────────
        var (statusCode, title) = exception switch
        {
            OperationCanceledException => (499, "Client Closed Request"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "Gateway Timeout"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Use the correlation ID to find details in logs.",
            Instance = httpContext.Request.Path,
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

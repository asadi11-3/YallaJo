using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using YallaJo.SharedKernel.Domain.Exceptions;

namespace YallaJo.Api.ExceptionHandlers;

/// <summary>
/// Maps <see cref="DomainException"/> subtypes raised by the Domain/Application
/// layers to the correct RFC-7807 status codes, keeping these out of the generic
/// 500 catch-all in <see cref="GlobalExceptionHandler"/>:
/// <list type="bullet">
///   <item><see cref="EntityNotFoundException"/> → 404 Not Found</item>
///   <item><see cref="BusinessRuleViolationException"/> → 409 Conflict</item>
///   <item><see cref="ConcurrencyException"/> → 409 Conflict</item>
/// </list>
/// Registered BEFORE <see cref="GlobalExceptionHandler"/> so these specific
/// mappings win. The domain <c>Code</c> is surfaced in the ProblemDetails
/// extensions for client-side disambiguation.
/// <para>
/// Note: 403 Forbidden (<see cref="UnauthorizedAccessException"/>) is already
/// handled by <see cref="GlobalExceptionHandler"/>; 400 (validation) by
/// <see cref="ValidationExceptionHandler"/>; and DB unique-violation 409 by
/// <see cref="DbUpdateExceptionHandler"/>.
/// </para>
/// </summary>
internal sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false;

        var (statusCode, title) = domainException switch
        {
            EntityNotFoundException        => (StatusCodes.Status404NotFound, "Not Found"),
            BusinessRuleViolationException => (StatusCodes.Status409Conflict, "Conflict"),
            ConcurrencyException           => (StatusCodes.Status409Conflict, "Conflict"),
            // Unknown future DomainException subtype → 422 (well-formed but
            // semantically rejected by domain rules) rather than a generic 500.
            _                              => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
        };

        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = domainException.Message,
            Instance = httpContext.Request.Path,
        };

        problemDetails.Extensions["code"] = domainException.Code;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

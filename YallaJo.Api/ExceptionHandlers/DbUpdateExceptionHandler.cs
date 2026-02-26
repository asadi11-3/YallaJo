using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace YallaJo.Api.ExceptionHandlers;

/// <summary>
/// Converts <see cref="DbUpdateException"/> caused by a unique constraint violation
/// (SQL Server error 2627 or 2601) into an RFC-7807 409 Conflict response,
/// keeping EF Core types out of the Application layer.
/// </summary>
internal sealed class DbUpdateExceptionHandler : IExceptionHandler
{
    private const int SqlDuplicateKeyError = 2627;
    private const int SqlUniqueIndexError = 2601;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException { InnerException: SqlException sqlEx })
            return false;

        if (sqlEx.Number is not (SqlDuplicateKeyError or SqlUniqueIndexError))
            return false;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title  = "Conflict",
            Detail = "A record with the same unique value already exists."
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace YallaJo.SharedKernel.Presentation;

/// <summary>
/// Single authoritative mapping from domain <see cref="Result"/> / <see cref="Result{T}"/>
/// to <see cref="IResult"/> HTTP responses.
///
/// Import this namespace in every Presentation project — eliminates the private
/// <c>ToApiResult</c> / <c>ToProblem</c> copies that used to live in each endpoint file,
/// including old per-module helpers (e.g. ContentCoreResultHelper).
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a typed result to <c>200 OK</c>, <c>201 Created</c>, or an RFC 7807 Problem response.
    /// The <c>201</c> branch is triggered when <see cref="Result{T}.Outcome"/> equals
    /// <see cref="Outcome.Created"/>.
    /// </summary>
    public static IResult ToApiResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    /// <summary>
    /// Maps a typed result to <c>201 Created</c> (with <c>Location</c> header) or a Problem response.
    /// Use when the caller must construct the URI of the newly created resource.
    /// </summary>
    public static IResult ToApiResult<T>(this Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), result.Value)
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    /// <summary>Maps a non-generic result to <c>200 OK</c> or a Problem response.</summary>
    public static IResult ToApiResult(this Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    // ── private ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Converts a failed outcome to an RFC 7807 Problem response.
    /// Errors take precedence; if none exist, the first message is used as detail.
    /// <para>
    /// When a result carries more than one <see cref="Error"/> (e.g. an aggregated
    /// pre-submit validation gate that reports every missing requirement at once),
    /// the full set is also emitted under the standard <c>errors</c> extension,
    /// keyed by error code. This keeps the first error as the title/detail (so
    /// single-error callers are unchanged) while letting clients surface each
    /// specific failure instead of only an umbrella message.
    /// </para>
    /// </summary>
    private static IResult ToProblem(
        Outcome outcome,
        IReadOnlyList<Error> errors,
        IReadOnlyList<string> messages)
    {
        if (errors.Count > 0)
        {
            var first = errors[0];

            // Expose every error so clients can render a complete checklist.
            // The first error is treated as an umbrella; the remainder (when
            // present) are the specific, actionable failures.
            IReadOnlyList<Error> detailErrors = errors.Count > 1 ? errors.Skip(1).ToList() : errors;
            var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["errors"] = detailErrors
                    .GroupBy(e => string.IsNullOrWhiteSpace(e.Code) ? "error" : e.Code, StringComparer.Ordinal)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Message).ToArray(),
                        StringComparer.Ordinal),
            };

            return Results.Problem(
                statusCode: (int)outcome,
                title: first.Code,
                detail: first.Message,
                extensions: extensions);
        }

        return Results.Problem(
            statusCode: (int)outcome,
            detail: messages.Count > 0 ? messages[0] : null);
    }
}

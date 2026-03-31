using Microsoft.AspNetCore.Http;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentCore.Presentation;

internal static class ContentCoreResultHelper
{
    internal static IResult ToApiResult<T>(Result<T> result) =>
        result.IsSuccess
            ? result.Outcome == Outcome.Created
                ? Results.Created((string?)null, result.Value)
                : Results.Ok(result.Value)
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    internal static IResult ToApiResult(Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblem(result.Outcome, result.Errors, result.Messages);

    private static IResult ToProblem(
        Outcome outcome,
        IReadOnlyList<Error> errors,
        IReadOnlyList<string> messages)
    {
        if (errors.Count > 0)
        {
            var first = errors[0];
            return Results.Problem(
                statusCode: (int)outcome,
                title: first.Code,
                detail: first.Message);
        }

        return Results.Problem(
            statusCode: (int)outcome,
            detail: messages.Count > 0 ? messages[0] : null);
    }
}

using Accounts.Application.Commands.RegisterUser;
using Accounts.Application.Commands.VerifyEmail;
using Accounts.Application.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Accounts.Presentation;

public static class AccountsEndpoints
{
    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapPost("/register", async (RegisterUserCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (result.IsFailure)
                return Results.BadRequest(new { result.Errors });
            return Results.Created($"/api/accounts/{result.Value}/profile", new { Id = result.Value });
        }).WithName("RegisterUser");

        group.MapPost("/{userId}/emails/{emailId}/verify",
            async (Guid userId, Guid emailId, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new VerifyEmailCommand(userId, emailId), ct);
                if (result.IsFailure)
                    return Results.BadRequest(new { result.Errors });
                return Results.Ok();
            }).WithName("VerifyEmail");

        group.MapGet("/{userId}/profile", async (Guid userId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetUserProfileQuery(userId), ct);
            if (result.IsFailure)
                return Results.NotFound();
            return Results.Ok(result.Value);
        }).WithName("GetUserProfile");

        return endpoints;
    }
}

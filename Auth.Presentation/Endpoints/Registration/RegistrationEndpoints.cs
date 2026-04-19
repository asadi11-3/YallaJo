using Auth.Application.Commands.Register;
using Auth.Presentation.Endpoints.Registration.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation;

namespace Auth.Presentation.Endpoints.Registration;

internal static class RegistrationEndpoints
{
    internal static void MapRegistrationEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new RegisterCommand(request.FirstName, request.LastName, request.Email, request.Password), ct);

            return result
                .Map(r => new RegisterResponse(
                    r.UserId,
                    "Registration successful. A verification email has been sent."))
                .ToApiResult();
        })
        .WithName("Register")
        .Produces<RegisterResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Register a new account — verification email will be sent")
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicies.RegisterPolicy);
    }
}

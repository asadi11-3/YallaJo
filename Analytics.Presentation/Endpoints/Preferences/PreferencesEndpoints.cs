using Analytics.Application.Commands.SetUserPreferences;
using Analytics.Application.Queries.GetUserPreferences;
using Analytics.Contracts.Authorization;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Analytics.Presentation.Endpoints.Preferences;

internal static class PreferencesEndpoints
{
    internal static void MapPreferencesEndpoints(this RouteGroupBuilder group)
    {
        var preferences = group.MapGroup("/analytics/preferences").WithTags("Analytics | Preferences");

        preferences.MapGet("/", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(new GetUserPreferencesQuery(currentUser.UserId.Value), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsPreferences")
        .Produces<UserPreferencesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Get the current user's recommendation preferences")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Read))
        .RequireAuthorization();

        preferences.MapPut("/", async (UpdatePreferencesRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var command = new SetUserPreferencesCommand(
                currentUser.UserId.Value,
                request.BudgetTier,
                request.IsFamilyTraveler,
                request.CurrentTripStage,
                request.Categories.Select(category => new SetUserPreferredCategory(category.CategoryId, category.PreferenceScore)).ToList());

            var result = await sender.Send(command, ct);
            return result.ToApiResult();
        })
        .WithName("UpdateAnalyticsPreferences")
        .Produces<UserPreferencesResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesValidationProblem()
        .WithSummary("Update the current user's recommendation preferences")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Update))
        .RequireAuthorization();
    }
}

public sealed record UpdatePreferencesRequest(string? BudgetTier, bool IsFamilyTraveler, string? CurrentTripStage, IReadOnlyList<UpdatePreferredCategoryRequest> Categories);
public sealed record UpdatePreferredCategoryRequest(Guid CategoryId, decimal PreferenceScore);

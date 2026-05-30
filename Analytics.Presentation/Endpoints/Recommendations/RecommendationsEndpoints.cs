using Analytics.Application.Commands.CancelGdprDeletion;
using Analytics.Application.Commands.CompleteExperiment;
using Analytics.Application.Commands.CreateBoostPackage;
using Analytics.Application.Commands.CreateCpcBoostPackage;
using Analytics.Application.Commands.CreateEditorialPin;
using Analytics.Application.Commands.CreateExperiment;
using Analytics.Application.Commands.CreateHolidayCalendar;
using Analytics.Application.Commands.CreateSeasonalityRule;
using Analytics.Application.Commands.DeactivateBoostPackage;
using Analytics.Application.Commands.DeactivateEditorialPin;
using Analytics.Application.Commands.DeactivateSeasonalityRule;
using Analytics.Application.Commands.MarkNotInterested;
using Analytics.Application.Commands.RecordSponsoredClick;
using Analytics.Application.Commands.RecordSuggestionMetric;
using Analytics.Application.Commands.RefreshSuggestionBatch;
using Analytics.Application.Commands.RequestGdprDeletion;
using Analytics.Application.Commands.SetEntityPhotogenic;
using Analytics.Application.Commands.StartExperiment;
using Analytics.Application.Commands.SubmitOnboardingResponses;
using Analytics.Application.Models;
using Analytics.Application.Queries.GetEntitySuggestions;
using Analytics.Application.Queries.GetHolidayCalendarByYear;
using Analytics.Application.Queries.GetItinerary;
using Analytics.Application.Queries.GetMetrics;
using Analytics.Application.Queries.GetRecommendations;
using Analytics.Application.Queries.GetReengagementSegment;
using Analytics.Application.Queries.GetSeasonalityRules;
using Analytics.Application.Queries.GetSimilarEntities;
using Analytics.Application.Queries.GetSuggestionBatches;
using Analytics.Application.Queries.GetUserDataExport;
using Analytics.Contracts.Authorization;
using Analytics.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Analytics.Presentation.Endpoints.Recommendations;

internal static class RecommendationsEndpoints
{
    internal static void MapRecommendationsEndpoints(this RouteGroupBuilder group)
    {
        var recommendations = group.MapGroup("/analytics/recommendations").WithTags("Analytics | Recommendations");

        recommendations.MapGet("/", async ([AsParameters] RecommendationsRequest request, ICurrentUser currentUser, ISender sender, HttpContext httpContext, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            var result = await sender.Send(new GetRecommendationsQuery(currentUser.UserId.Value, request.Language, request.Limit, request.HalalOnly, acceptLanguage, request.ShowAllPrices), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsRecommendations")
        .Produces<RecommendationsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Get personalized recommendations for the current user")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Recommendation, AppAction.Read))
        .RequireAuthorization();

        recommendations.MapGet("/similar/{entityId:guid}", async (Guid entityId, [AsParameters] SimilarEntitiesRequest request, ISender sender, HttpContext httpContext, CancellationToken ct) =>
        {
            var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            var result = await sender.Send(new GetSimilarEntitiesQuery(request.SourceKind, entityId, request.Limit, request.Language, request.HalalOnly, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsSimilarEntities")
        .Produces<SimilarEntitiesResponse>(StatusCodes.Status200OK)
        .WithSummary("Get entities similar to the specified entity")
        .AllowAnonymous();

        recommendations.MapGet("/for/{kind}/{entityId:guid}", async (EntityType kind, Guid entityId, [AsParameters] EntitySuggestionsRequest request, ISender sender, HttpContext httpContext, CancellationToken ct) =>
        {
            var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            var result = await sender.Send(new GetEntitySuggestionsQuery(kind, entityId, request.Context, request.Limit, request.Language, request.HalalOnly, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsEntitySuggestions")
        .Produces<EntitySuggestionsResponse>(StatusCodes.Status200OK)
        .WithSummary("Get contextual suggestions for an entity")
        .AllowAnonymous();

        // 4.1: Onboarding quiz
        recommendations.MapPost("/onboarding", async (SubmitOnboardingRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var responses = request.InterestedEntityIds
                .Select(id => new OnboardingResponseItem(id.Kind, id.EntityId, true))
                .Concat(request.NotInterestedEntityIds
                    .Select(id => new OnboardingResponseItem(id.Kind, id.EntityId, false)))
                .ToList();
            var result = await sender.Send(new SubmitOnboardingResponsesCommand(currentUser.UserId.Value, responses), ct);
            return result.ToApiResult();
        })
        .WithName("SubmitOnboardingResponses")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Submit cold-start onboarding quiz responses")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Update))
        .RequireAuthorization();

        // 4.2: Not interested feedback
        recommendations.MapPost("/not-interested", async (MarkNotInterestedRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var result = await sender.Send(new MarkNotInterestedCommand(currentUser.UserId.Value, request.EntityKind, request.EntityId), ct);
            return result.ToApiResult();
        })
        .WithName("MarkNotInterested")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Mark an entity as not interested (excluded for 90 days)")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Update))
        .RequireAuthorization();

        var batches = group.MapGroup("/analytics/admin/batches").WithTags("Analytics | Recommendation Batches");

        batches.MapPost("/refresh", async (RefreshSuggestionBatchRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshSuggestionBatchCommand(request.SourceKind, request.SourceId, request.Context), ct);
            return result.ToApiResult();
        })
        .WithName("RefreshAnalyticsSuggestionBatch")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Refresh a recommendation suggestion batch")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Batch, AppAction.Refresh))
        .RequireAuthorization();

        batches.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSuggestionBatchesQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value.Batches) : result.ToApiResult();
        })
        .WithName("ListAnalyticsSuggestionBatches")
        .Produces<IReadOnlyList<SuggestionBatchDto>>(StatusCodes.Status200OK)
        .WithSummary("List recommendation suggestion batches")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Batch, AppAction.Read))
        .RequireAuthorization();

        // 3.6: Boost package admin endpoints
        var boosts = group.MapGroup("/analytics/admin/boosts").WithTags("Analytics | Boost Packages");

        boosts.MapPost("/", async (CreateBoostPackageRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateBoostPackageCommand(request.ProviderId, request.EntityKind, request.EntityId, request.Multiplier, request.StartsAt, request.ExpiresAt), ct);
            return result.IsSuccess ? Results.Ok(new { result.Value.Id }) : result.ToApiResult();
        })
        .WithName("CreateAnalyticsBoostPackage")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Create a boost package for a provider entity")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.BoostPackage, AppAction.Create))
        .RequireAuthorization();

        boosts.MapDelete("/{boostId:guid}", async (Guid boostId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateBoostPackageCommand(boostId), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateAnalyticsBoostPackage")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate a boost package")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.BoostPackage, AppAction.Delete))
        .RequireAuthorization();

        // 3.7: Editorial pin admin endpoints
        var pins = group.MapGroup("/analytics/admin/pins").WithTags("Analytics | Editorial Pins");

        pins.MapPost("/", async (CreateEditorialPinRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateEditorialPinCommand(request.EntityKind, request.EntityId, request.Position, request.Context, request.BadgeText, request.ExpiresAt), ct);
            return result.IsSuccess ? Results.Ok(new { result.Value.Id }) : result.ToApiResult();
        })
        .WithName("CreateAnalyticsEditorialPin")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Create an editorial pin")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.EditorialPin, AppAction.Create))
        .RequireAuthorization();

        pins.MapDelete("/{pinId:guid}", async (Guid pinId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateEditorialPinCommand(pinId), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateAnalyticsEditorialPin")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate an editorial pin")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.EditorialPin, AppAction.Delete))
        .RequireAuthorization();

        // 5.4: Itinerary planner
        recommendations.MapGet("/itinerary", async ([AsParameters] ItineraryRequest request, ISender sender, HttpContext httpContext, CancellationToken ct) =>
        {
            var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.FirstOrDefault();
            var result = await sender.Send(new GetItineraryQuery(
                request.FromDate, request.ToDate, request.StartLatitude, request.StartLongitude,
                request.Interests, request.Language, request.HalalOnly, acceptLanguage), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsItinerary")
        .Produces<ItineraryResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Get a multi-day itinerary plan for Jordan")
        .AllowAnonymous();

        // 5.1: Seasonality rules admin CRUD
        var seasonality = group.MapGroup("/analytics/admin/seasonality").WithTags("Analytics | Seasonality Rules");

        seasonality.MapPost("/", async (CreateSeasonalityRuleRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateSeasonalityRuleCommand(request.PlaceId, request.MonthStart, request.MonthEnd, request.Multiplier, request.Description), ct);
            return result.IsSuccess ? Results.Ok(new { result.Value.Id }) : result.ToApiResult();
        })
        .WithName("CreateAnalyticsSeasonalityRule")
        .Produces(StatusCodes.Status200OK)
        .WithSummary("Create a seasonality boost rule")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.SeasonalityRule, AppAction.Create))
        .RequireAuthorization();

        seasonality.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSeasonalityRulesQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value.Rules) : result.ToApiResult();
        })
        .WithName("ListAnalyticsSeasonalityRules")
        .Produces<IEnumerable<SeasonalityRuleDto>>(StatusCodes.Status200OK)
        .WithSummary("List active seasonality rules")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Batch, AppAction.Read))
        .RequireAuthorization();

        seasonality.MapDelete("/{ruleId:guid}", async (Guid ruleId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeactivateSeasonalityRuleCommand(ruleId), ct);
            return result.ToApiResult();
        })
        .WithName("DeactivateAnalyticsSeasonalityRule")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Deactivate a seasonality rule")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.SeasonalityRule, AppAction.Delete))
        .RequireAuthorization();

        // 5.2: Holiday calendar admin CRUD
        var holidays = group.MapGroup("/analytics/admin/holidays").WithTags("Analytics | Holiday Calendar");

        holidays.MapPost("/", async (CreateHolidayCalendarRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateHolidayCalendarCommand(request.HolidayName, request.StartDate, request.EndDate, request.Year, request.BoostRulesJson), ct);
            return result.IsSuccess ? Results.Ok(new { result.Value.Id }) : result.ToApiResult();
        })
        .WithName("CreateAnalyticsHolidayCalendar")
        .Produces(StatusCodes.Status200OK)
        .WithSummary("Create a holiday calendar entry")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.HolidayCalendar, AppAction.Create))
        .RequireAuthorization();

        holidays.MapGet("/{year:int}", async (int year, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetHolidayCalendarByYearQuery(year), ct);
            return result.IsSuccess ? Results.Ok(result.Value.Holidays) : result.ToApiResult();
        })
        .WithName("ListAnalyticsHolidayCalendar")
        .Produces<IEnumerable<HolidayCalendarDto>>(StatusCodes.Status200OK)
        .WithSummary("List holiday calendar entries for a year")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Batch, AppAction.Read))
        .RequireAuthorization();

        // 5.5: Photogenic admin endpoint
        var entities = group.MapGroup("/analytics/admin/entities").WithTags("Analytics | Entity Admin");

        entities.MapPut("/{kind}/{entityId:guid}/photogenic", async (EntityType kind, Guid entityId, SetPhotogenicRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetEntityPhotogenicCommand(kind, entityId, request.IsPhotogenic), ct);
            return result.ToApiResult();
        })
        .WithName("SetAnalyticsEntityPhotogenic")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Set photogenic hotspot flag on an entity")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Photogenic, AppAction.Update))
        .RequireAuthorization();

        // ── Phase 6: V3 Marketplace + Ops ──

        // 6.1: Sponsored CPC bid creation
        boosts.MapPost("/cpc", async (CreateCpcBidRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateCpcBoostPackageCommand(request.ProviderId, request.EntityKind, request.EntityId, request.BidPerClick, request.DailyBudgetCap, request.StartsAt, request.ExpiresAt), ct);
            return result.IsSuccess ? Results.Ok(new { result.Value.Id }) : result.ToApiResult();
        })
        .WithName("CreateAnalyticsCpcBid")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Create a CPC (cost-per-click) sponsored bid")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.BoostPackage, AppAction.Create))
        .RequireAuthorization();

        // 6.1: Record a sponsored click
        recommendations.MapPost("/sponsored-click", async (RecordSponsoredClickRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordSponsoredClickCommand(
                request.BidId, currentUser.UserId, null,
                request.SourceKind, request.SourceId, request.Position, request.DwellTimeSeconds), ct);
            return result.ToApiResult();
        })
        .WithName("RecordSponsoredClick")
        .Produces(StatusCodes.Status200OK)
        .WithSummary("Record a click on a sponsored placement")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Recommendation, AppAction.Read))
        .RequireAuthorization();

        // 6.2: A/B experiments admin
        var experiments = group.MapGroup("/analytics/admin/experiments").WithTags("Analytics | Experiments");

        experiments.MapPost("/", async (CreateExperimentRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateExperimentCommand(request.Name, request.StartsAt, request.ExpiresAt, request.TrafficPercent, request.VariantsJson), ct);
            return result.IsSuccess ? Results.Ok(new { result.Value.Id }) : result.ToApiResult();
        })
        .WithName("CreateAnalyticsExperiment")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Create an A/B experiment")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Experiment, AppAction.Create))
        .RequireAuthorization();

        experiments.MapPut("/{experimentId:guid}/start", async (Guid experimentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new StartExperimentCommand(experimentId), ct);
            return result.ToApiResult();
        })
        .WithName("StartAnalyticsExperiment")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Start an A/B experiment")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Experiment, AppAction.Update))
        .RequireAuthorization();

        experiments.MapPut("/{experimentId:guid}/complete", async (Guid experimentId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CompleteExperimentCommand(experimentId), ct);
            return result.ToApiResult();
        })
        .WithName("CompleteAnalyticsExperiment")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Complete an A/B experiment")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Experiment, AppAction.Update))
        .RequireAuthorization();

        // 6.3: Suggestion metric tracking
        recommendations.MapPost("/metrics", async (RecordSuggestionMetricRequest request, ISender sender, ICurrentUser currentUser, CancellationToken ct) =>
        {
            var result = await sender.Send(new RecordSuggestionMetricCommand(
                request.BatchId, request.RecommendationCacheId, currentUser.UserId,
                request.Position, request.Stage, null, request.ExperimentVariant), ct);
            return result.ToApiResult();
        })
        .WithName("RecordSuggestionMetric")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Record an impression/click/booking event on a recommendation")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Recommendation, AppAction.Read))
        .RequireAuthorization();

        // 6.3: Admin metrics endpoint
        var metrics = group.MapGroup("/analytics/admin/metrics").WithTags("Analytics | Metrics");

        metrics.MapGet("/", async ([AsParameters] GetMetricsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMetricsQuery(request.From, request.To, request.Context), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsMetrics")
        .Produces<MetricsResponse>(StatusCodes.Status200OK)
        .WithSummary("Get CTR / conversion funnel metrics")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Batch, AppAction.Read))
        .RequireAuthorization();

        // 6.6: Re-engagement segments
        var segments = group.MapGroup("/analytics/admin/segments").WithTags("Analytics | Segments");

        segments.MapGet("/", async ([AsParameters] GetSegmentRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetReengagementSegmentQuery(
                request.Rule, request.EntityKind, request.EntityId), ct);
            return result.ToApiResult();
        })
        .WithName("GetAnalyticsReengagementSegment")
        .Produces<SegmentResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Query a re-engagement segment by rule")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Batch, AppAction.Read))
        .RequireAuthorization();

        // 6.7: GDPR delete-my-data
        recommendations.MapDelete("/me", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RequestGdprDeletionCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("RequestAnalyticsGdprDeletion")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithSummary("Request deletion of all personal analytics data (30-day window)")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Update))
        .RequireAuthorization();

        // 6.7: Cancel GDPR deletion
        recommendations.MapPost("/me/cancel-deletion", async (ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CancelGdprDeletionCommand(currentUser.UserId!.Value), ct);
            return result.ToApiResult();
        })
        .WithName("CancelAnalyticsGdprDeletion")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Cancel a pending GDPR deletion request")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Update))
        .RequireAuthorization();

        // GDPR data export (portability right)
        recommendations.MapGet("/me/export", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserDataExportQuery(), ct);
            return result.ToApiResult();
        })
        .WithName("ExportAnalyticsUserData")
        .Produces<UserDataExportDto>(StatusCodes.Status200OK)
        .WithSummary("Export all personal analytics data (GDPR portability)")
        .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Preference, AppAction.Read))
        .RequireAuthorization();
    }

}

public sealed record RecommendationsRequest(string? Language = null, int Limit = 20, bool HalalOnly = false, bool ShowAllPrices = false);
public sealed record SimilarEntitiesRequest(EntityType SourceKind = EntityType.Tour, string? Language = null, int Limit = 10, bool HalalOnly = false);
public sealed record EntitySuggestionsRequest(SuggestionContext Context = SuggestionContext.BecauseYouViewed, string? Language = null, int Limit = 10, bool HalalOnly = false);
public sealed record RefreshSuggestionBatchRequest(EntityType SourceKind, Guid SourceId, SuggestionContext Context);
public sealed record CreateBoostPackageRequest(Guid ProviderId, EntityType EntityKind, Guid EntityId, decimal Multiplier, DateTime StartsAt, DateTime ExpiresAt);
public sealed record CreateEditorialPinRequest(EntityType EntityKind, Guid EntityId, int Position, SuggestionContext Context, string? BadgeText = null, DateTime? ExpiresAt = null);
public sealed record MarkNotInterestedRequest(EntityType EntityKind, Guid EntityId);
public sealed record SubmitOnboardingRequest(IReadOnlyList<EntityRefDto> InterestedEntityIds, IReadOnlyList<EntityRefDto> NotInterestedEntityIds);
public sealed record EntityRefDto(EntityType Kind, Guid EntityId);

// Phase 5 DTOs
public sealed record ItineraryRequest(DateOnly FromDate, DateOnly ToDate, decimal? StartLatitude = null, decimal? StartLongitude = null, string? Interests = null, string? Language = null, bool HalalOnly = false);
public sealed record CreateSeasonalityRuleRequest(Guid PlaceId, int MonthStart, int MonthEnd, decimal Multiplier, string? Description = null);
public sealed record CreateHolidayCalendarRequest(string HolidayName, DateOnly StartDate, DateOnly EndDate, int Year, string? BoostRulesJson = null);
public sealed record SetPhotogenicRequest(bool IsPhotogenic);

// Phase 6 DTOs
public sealed record CreateCpcBidRequest(Guid ProviderId, EntityType EntityKind, Guid EntityId, decimal BidPerClick, decimal DailyBudgetCap, DateTime StartsAt, DateTime ExpiresAt);
public sealed record RecordSponsoredClickRequest(Guid BidId, EntityType SourceKind, Guid SourceId, int Position, int DwellTimeSeconds);
public sealed record CreateExperimentRequest(string Name, DateTime StartsAt, DateTime ExpiresAt, int TrafficPercent, string VariantsJson);
public sealed record RecordSuggestionMetricRequest(Guid? BatchId, Guid? RecommendationCacheId, int Position, string Stage, string? ExperimentVariant = null);
public sealed record GetMetricsRequest(DateTime From, DateTime To, string? Context = null);
public sealed record GetSegmentRequest(string Rule, EntityType? EntityKind = null, Guid? EntityId = null);

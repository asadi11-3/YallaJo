using System.Security.Claims;
using Analytics.Application;
using Analytics.Contracts.Authorization;
using Analytics.Presentation.Endpoints.Preferences;
using Analytics.Presentation.Endpoints.Recommendations;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace Analytics.Presentation;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1").WithTags("Analytics");

        group.MapRecommendationsEndpoints();
        group.MapPreferencesEndpoints();

        group.MapPost("/interactions", async (RecordInteractionRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
        {
            await sender.Send(new RecordInteractionCommand(request.UserId, request.SessionId, request.EntityType, request.EntityId, request.InteractionType, http.Connection.RemoteIpAddress?.ToString(), http.Request.Headers.UserAgent.ToString()), ct);
            return Results.Accepted();
        })
            .WithName("RecordInteraction").AllowAnonymous();

        group.MapGet("/admin/interactions", async ([AsParameters] InteractionListRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminInteractionsQuery(request.UserId, request.EntityType, request.EntityId, request.InteractionType, request.From, request.To, request.AfterId, request.PageSize), ct)).ToApiResult())
            .WithName("GetAdminInteractions").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Interaction, AppAction.Read));

        group.MapGet("/admin/interactions/user/{userId:guid}", async (Guid userId, long? afterId, int pageSize, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUserInteractionsQuery(userId, afterId, pageSize), ct)).ToApiResult())
            .WithName("GetUserInteractions").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Interaction, AppAction.Read));

        group.MapGet("/popular/tours", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetPopularEntitiesQuery("Tour"), ct)).ToApiResult()).WithName("GetPopularTours").AllowAnonymous();
        group.MapGet("/popular/places", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetPopularEntitiesQuery("Place"), ct)).ToApiResult()).WithName("GetPopularPlaces").AllowAnonymous();
        group.MapGet("/popular/businesses", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetPopularEntitiesQuery("Business"), ct)).ToApiResult()).WithName("GetPopularBusinesses").AllowAnonymous();
        group.MapGet("/trending", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetTrendingQuery(), ct)).ToApiResult()).WithName("GetTrending").AllowAnonymous();

        group.MapGet("/admin/dashboard", async (ISender sender, CancellationToken ct) => (await sender.Send(new GetAdminDashboardOverviewQuery(), ct)).ToApiResult()).WithName("GetAdminDashboard").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read));
        group.MapGet("/admin/dashboard/revenue", async (DateTime? from, DateTime? to, ISender sender, CancellationToken ct) => (await sender.Send(new GetAdminRevenueDashboardQuery(from, to), ct)).ToApiResult()).WithName("GetAdminRevenueDashboard").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read));
        group.MapGet("/admin/dashboard/bookings", async (DateTime? from, DateTime? to, ISender sender, CancellationToken ct) => (await sender.Send(new GetAdminBookingsDashboardQuery(from, to), ct)).ToApiResult()).WithName("GetAdminBookingsDashboard").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read));
        group.MapGet("/admin/dashboard/users", async (DateTime? from, DateTime? to, ISender sender, CancellationToken ct) => (await sender.Send(new GetAdminUsersDashboardQuery(from, to), ct)).ToApiResult()).WithName("GetAdminUsersDashboard").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read));

        group.MapGet("/provider/dashboard", async (HttpContext http, ISender sender, CancellationToken ct) => (await sender.Send(new GetProviderDashboardQuery(GetProviderId(http)), ct)).ToApiResult()).WithName("GetProviderDashboard").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.ProviderDashboard, AppAction.Read));
        group.MapGet("/provider/analytics", async (DateTime? from, DateTime? to, HttpContext http, ISender sender, CancellationToken ct) => (await sender.Send(new GetProviderAnalyticsQuery(GetProviderId(http), from, to), ct)).ToApiResult()).WithName("GetProviderAnalytics").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.ProviderDashboard, AppAction.Read));
        group.MapGet("/provider/my-tours", async (long? afterId, int pageSize, HttpContext http, ISender sender, CancellationToken ct) => (await sender.Send(new GetProviderMyToursQuery(GetProviderId(http), afterId, pageSize), ct)).ToApiResult()).WithName("GetProviderMyTours").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.ProviderDashboard, AppAction.Read));

        group.MapGet("/admin/audit-logs", async ([AsParameters] AuditLogListRequest request, ISender sender, CancellationToken ct) => (await sender.Send(new GetAuditLogsQuery(request.EntityType, request.EntityId, request.UserId, request.Action, request.From, request.To, request.AfterId, request.PageSize), ct)).ToApiResult()).WithName("GetAdminAuditLogs").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AuditLog, AppAction.Read));
        group.MapPost("/admin/audit-logs/{id:long}/redact", async (long id, RedactAuditLogRequest request, HttpContext http, ISender sender, CancellationToken ct) => (await sender.Send(new RedactAuditLogCommand(id, request.AdminUserId ?? GetUserId(http), request.Reason), ct)).ToApiResult()).WithName("RedactAuditLog").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AuditLog, AppAction.Redact));
        group.MapGet("/admin/audit-logs/export", async (DateTime from, DateTime to, ISender sender, CancellationToken ct) => (await sender.Send(new ExportAuditLogsQuery(from, to), ct)).ToApiResult()).WithName("ExportAuditLogs").WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AuditLog, AppAction.Export));

        return endpoints;
    }

    private static Guid GetProviderId(HttpContext http) => Guid.TryParse(http.User.FindFirstValue("provider_id") ?? http.User.FindFirstValue("providerId"), out var id) ? id : Guid.Empty;
    private static Guid GetUserId(HttpContext http) => Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}

public sealed record RecordInteractionRequest(Guid? UserId, string? SessionId, string EntityType, Guid EntityId, string InteractionType);
public sealed record RedactAuditLogRequest(Guid? AdminUserId, string Reason);
public sealed record InteractionListRequest(Guid? UserId, string? EntityType, Guid? EntityId, string? InteractionType, DateTime? From, DateTime? To, long? AfterId, int PageSize = 50);
public sealed record AuditLogListRequest(string? EntityType, Guid? EntityId, Guid? UserId, string? Action, DateTime? From, DateTime? To, long? AfterId, int PageSize = 100);

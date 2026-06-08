using System.Security.Claims;
using Analytics.Application.Commands.RecordInteraction;
using Analytics.Application.Commands.RedactAuditLog;
using Analytics.Application.Queries.ExportAuditLogs;
using Analytics.Application.Queries.GetAdminBookingsDashboard;
using Analytics.Application.Queries.GetAdminDashboardOverview;
using Analytics.Application.Queries.GetAdminInteractions;
using Analytics.Application.Queries.GetAdminRevenueDashboard;
using Analytics.Application.Queries.GetAdminUsersDashboard;
using Analytics.Application.Queries.GetAuditLogs;
using Analytics.Application.Queries.GetPopularEntities;
using Analytics.Application.Queries.GetProviderAnalytics;
using Analytics.Application.Queries.GetProviderDashboard;
using Analytics.Application.Queries.GetProviderMyTours;
using Analytics.Application.Queries.GetTrending;
using Analytics.Application.Queries.GetUserInteractions;
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
            .WithName("RecordInteraction")
            .WithSummary("Record a user interaction event.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Interaction, AppAction.Create))
            .RequireAuthorization();

        group.MapGet("/admin/interactions", async ([AsParameters] InteractionListRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminInteractionsQuery(request.UserId, request.EntityType, request.EntityId, request.InteractionType, request.From, request.To, request.AfterId, request.PageSize), ct)).ToApiResult())
            .WithName("GetAdminInteractions")
            .WithSummary("Admin: list user interaction events.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Interaction, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/admin/interactions/user/{userId:guid}", async (Guid userId, long? afterId, int pageSize, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetUserInteractionsQuery(userId, afterId, pageSize), ct)).ToApiResult())
            .WithName("GetUserInteractions")
            .WithSummary("Admin: list interactions for a specific user.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.Interaction, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/popular/tours", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPopularEntitiesQuery("Tour"), ct)).ToApiResult())
            .WithName("GetPopularTours")
            .WithSummary("List the most popular tours.")
            .AllowAnonymous();

        group.MapGet("/popular/places", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPopularEntitiesQuery("Place"), ct)).ToApiResult())
            .WithName("GetPopularPlaces")
            .WithSummary("List the most popular places.")
            .AllowAnonymous();

        group.MapGet("/popular/businesses", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetPopularEntitiesQuery("Business"), ct)).ToApiResult())
            .WithName("GetPopularBusinesses")
            .WithSummary("List the most popular businesses.")
            .AllowAnonymous();

        group.MapGet("/trending", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetTrendingQuery(), ct)).ToApiResult())
            .WithName("GetTrending")
            .WithSummary("List currently trending entities.")
            .AllowAnonymous();

        group.MapGet("/admin/dashboard", async (ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminDashboardOverviewQuery(), ct)).ToApiResult())
            .WithName("GetAdminDashboard")
            .WithSummary("Admin: analytics dashboard overview.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/admin/dashboard/revenue", async (DateTime? from, DateTime? to, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminRevenueDashboardQuery(from, to), ct)).ToApiResult())
            .WithName("GetAdminRevenueDashboard")
            .WithSummary("Admin: revenue dashboard for a date range.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/admin/dashboard/bookings", async (DateTime? from, DateTime? to, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminBookingsDashboardQuery(from, to), ct)).ToApiResult())
            .WithName("GetAdminBookingsDashboard")
            .WithSummary("Admin: bookings dashboard for a date range.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/admin/dashboard/users", async (DateTime? from, DateTime? to, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAdminUsersDashboardQuery(from, to), ct)).ToApiResult())
            .WithName("GetAdminUsersDashboard")
            .WithSummary("Admin: users dashboard for a date range.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AdminDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/provider/dashboard", async (HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProviderDashboardQuery(GetProviderId(http)), ct)).ToApiResult())
            .WithName("GetProviderDashboard")
            .WithSummary("Get the current provider's analytics dashboard.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.ProviderDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/provider/analytics", async (DateTime? from, DateTime? to, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProviderAnalyticsQuery(GetProviderId(http), from, to), ct)).ToApiResult())
            .WithName("GetProviderAnalytics")
            .WithSummary("Get the current provider's analytics for a date range.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.ProviderDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/provider/my-tours", async (long? afterId, int pageSize, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProviderMyToursQuery(GetProviderId(http), afterId, pageSize), ct)).ToApiResult())
            .WithName("GetProviderMyTours")
            .WithSummary("List the current provider's tours with performance metrics.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.ProviderDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/guide/dashboard", async (DateTime? from, DateTime? to, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProviderDashboardQuery(GetUserId(http)), ct)).ToApiResult())
            .WithName("GetGuideDashboard")
            .WithSummary("Get the current guide's analytics dashboard.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.GuideDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/guide/analytics", async (DateTime? from, DateTime? to, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProviderAnalyticsQuery(GetUserId(http), from, to), ct)).ToApiResult())
            .WithName("GetGuideAnalytics")
            .WithSummary("Get the current guide's analytics for a date range.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.GuideDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/guide/my-tours", async (long? afterId, int pageSize, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetProviderMyToursQuery(GetUserId(http), afterId, pageSize), ct)).ToApiResult())
            .WithName("GetGuideMyTours")
            .WithSummary("List the current guide's tours with performance metrics.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.GuideDashboard, AppAction.Read))
            .RequireAuthorization();

        group.MapGet("/admin/audit-logs", async ([AsParameters] AuditLogListRequest request, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAuditLogsQuery(request.EntityType, request.EntityId, request.UserId, request.Action, request.From, request.To, request.AfterId, request.PageSize), ct)).ToApiResult())
            .WithName("GetAdminAuditLogs")
            .WithSummary("Admin: list audit logs.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AuditLog, AppAction.Read))
            .RequireAuthorization();

        group.MapPost("/admin/audit-logs/{id:long}/redact", async (long id, RedactAuditLogRequest request, HttpContext http, ISender sender, CancellationToken ct) =>
            (await sender.Send(new RedactAuditLogCommand(id, GetUserId(http), request.Reason), ct)).ToApiResult())
            .WithName("RedactAuditLog")
            .WithSummary("Admin: redact an audit log entry.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AuditLog, AppAction.Redact))
            .RequireAuthorization();

        group.MapGet("/admin/audit-logs/export", async (DateTime from, DateTime to, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportAuditLogsQuery(from, to), ct);
            if (result.IsFailure)
            {
                return result.ToApiResult();
            }

            return Results.Stream(
                async stream =>
                {
                    await using var writer = new StreamWriter(stream, leaveOpen: true);
                    await foreach (var line in result.Value!.WithCancellation(ct))
                    {
                        await writer.WriteLineAsync(line.AsMemory(), ct);
                        await writer.FlushAsync(ct);
                    }
                },
                contentType: "text/csv",
                fileDownloadName: "audit-logs.csv");
        })
            .WithName("ExportAuditLogs")
            .WithSummary("Admin: export audit logs for a date range.")
            .WithMetadata(new MustHavePermissionAttribute(AnalyticsFeatures.AuditLog, AppAction.Export))
            .RequireAuthorization();

        return endpoints;
    }

    private static Guid GetProviderId(HttpContext http) => Guid.TryParse(http.User.FindFirstValue("provider_id") ?? http.User.FindFirstValue("providerId"), out var id) ? id : Guid.Empty;
    private static Guid GetUserId(HttpContext http) => Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}

public sealed record RecordInteractionRequest(Guid? UserId, string? SessionId, string EntityType, Guid EntityId, string InteractionType);
public sealed record RedactAuditLogRequest(string Reason);
public sealed record InteractionListRequest(Guid? UserId, string? EntityType, Guid? EntityId, string? InteractionType, DateTime? From, DateTime? To, long? AfterId, int PageSize = 50);
public sealed record AuditLogListRequest(string? EntityType, Guid? EntityId, Guid? UserId, string? Action, DateTime? From, DateTime? To, long? AfterId, int PageSize = 100);

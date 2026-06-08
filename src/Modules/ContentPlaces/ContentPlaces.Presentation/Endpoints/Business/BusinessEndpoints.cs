using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateAccessibilityFeatures;
using ContentPlaces.Application.Commands.AccessibilityFeature.UpdateBusinessAccessibilityFeatures;
using ContentPlaces.Application.Commands.Business.ApproveBusiness;
using ContentPlaces.Application.Commands.Business.CreateBusiness;
using ContentPlaces.Application.Commands.Business.DeleteBusiness;
using ContentPlaces.Application.Commands.Business.RejectBusiness;
using ContentPlaces.Application.Commands.Business.ReinstateBusiness;
using ContentPlaces.Application.Commands.Business.RequestMoreDocs;
using ContentPlaces.Application.Commands.Business.ResubmitBusiness;
using ContentPlaces.Application.Commands.Business.SuspendBusiness;
using ContentPlaces.Application.Commands.Business.UpdateBusiness;
using ContentPlaces.Application.Commands.BusinessHours.SetBusinessHours;
using ContentPlaces.Application.Queries.AccessibilityFeature.Common;
using ContentPlaces.Application.Queries.AccessibilityFeature.GetBusinessAccessibilityFeatures;
using ContentPlaces.Application.Queries.Business.Common;
using ContentPlaces.Application.Queries.Business.GetBusinessById;
using ContentPlaces.Application.Queries.Business.GetMyBusinesses;
using ContentPlaces.Application.Queries.Business.GetBusinessHours;
using ContentPlaces.Application.Queries.Business.GetNearbyBusinesses;
using ContentPlaces.Application.Queries.Business.ListPlaceBusinesses;
using ContentPlaces.Application.Queries.Business.SearchBusinesses;
using ContentPlaces.Contracts.Authorization;
using ContentPlaces.Presentation.Endpoints.Business.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Security.Contracts.Authorization;
using System.Security.Claims;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Presentation;
using YallaJo.SharedKernel.Presentation.Authorization;

namespace ContentPlaces.Presentation.Endpoints.Business;

internal static class BusinessEndpoints
{
    internal static void MapBusinessEndpoints(RouteGroupBuilder group)
    {
        // ── Businesses ─────────────────────────────────────────────────────────
        var businesses = group.MapGroup("").WithTags("ContentPlaces | Businesses");

        // GET /places/businesses — global list of approved businesses (public).
        // F6 fix: prior to this, /places/businesses fell through to /places/{slug}
        // and returned Place.NotFound. Reuses SearchBusinessesQuery with Query=null
        // so caching/filters/handler are shared with /places/businesses/search.
        businesses.MapGet("/places/businesses", async (
            HttpContext http,
            ISender sender,
            string? businessType = null,
            string? city = null,
            string? country = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new SearchBusinessesQuery(page, pageSize, Query: null, businessType, city, country),
                http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("ListBusinesses")
        .Produces<PaginatedResult<BusinessSummaryDto>>(StatusCodes.Status200OK)
        .WithSummary("List businesses (public; returns Approved only; optional businessType/city/country filters; max pageSize 50).")
        .AllowAnonymous();

        // GET /places/{id}/businesses — list all businesses for a place
        businesses.MapGet("/places/{id:guid}/businesses", async (
            Guid id,
            HttpContext http,
            ISender sender,
            int page = 1,
            int pageSize = 20) =>
        {
            var (userId, isAdmin) = ExtractUser(http);
            var result = await sender.Send(
                new ListPlaceBusinessesQuery(id, userId, isAdmin, page, pageSize),
                http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("ListPlaceBusinesses")
        .Produces<PaginatedResult<BusinessSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("List businesses for a place (public sees Approved only; owner/admin sees all statuses)")
        .AllowAnonymous();

        // GET /places/businesses/{id} — get single business
        businesses.MapGet("/places/businesses/{id:guid}", async (
            Guid id,
            HttpContext http,
            ISender sender) =>
        {
            var (userId, isAdmin) = ExtractUser(http);
            var result = await sender.Send(
                new GetBusinessByIdQuery(id, userId, isAdmin),
                http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("GetBusinessById")
        .Produces<BusinessDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get a business by Id (public sees Approved only; owner/admin sees all statuses)")
        .AllowAnonymous();

        // GET /places/businesses/search — search businesses with filters
        businesses.MapGet("/places/businesses/search", async (
            HttpContext http,
            ISender sender,
            string? query = null,
            string? businessType = null,
            string? city = null,
            string? country = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new SearchBusinessesQuery(page, pageSize, query, businessType, city, country),
                http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("SearchBusinesses")
        .Produces<PaginatedResult<BusinessSummaryDto>>(StatusCodes.Status200OK)
        .WithSummary("Search businesses by name, type, city, or country (public; returns Approved only)")
        .AllowAnonymous();

        // GET /places/businesses/nearby — find businesses near coordinates
        businesses.MapGet("/places/businesses/nearby", async (
            HttpContext http,
            ISender sender,
            double lat,
            double lng,
            double radiusKm = 10,
            int pageSize = 10) =>
        {
            var result = await sender.Send(
                new GetNearbyBusinessesQuery(lat, lng, radiusKm, pageSize),
                http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("GetNearbyBusinesses")
        .Produces<IReadOnlyList<NearbyBusinessSummaryDto>>(StatusCodes.Status200OK)
        .WithSummary("Get nearby businesses using Haversine formula (max 100km radius)")
        .AllowAnonymous();

        // POST /places/businesses — create a business
        businesses.MapPost("/places/businesses", async (
            CreateBusinessRequest request,
            ISender sender) =>
        {
            var result = await sender.Send(new CreateBusinessCommand(
                Name: request.Name,
                Slug: request.Slug,
                BusinessType: request.BusinessType,
                PlaceId: request.PlaceId,
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                Description: request.Description,
                Address: request.Address,
                City: request.City,
                Country: request.Country,
                PostalCode: request.PostalCode,
                Phone: request.Phone,
                Email: request.Email,
                Website: request.Website,
                LicenseNumber: request.LicenseNumber,
                TaxId: request.TaxId,
                IsHalal: request.IsHalal,
                HasVegetarianOptions: request.HasVegetarianOptions,
                HasAlcoholFreeArea: request.HasAlcoholFreeArea));
            return result.ToApiResult();
        })
        .WithName("CreateBusiness")
        .Produces<CreateBusinessResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Create a business linked to a Place (status starts as Pending)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Create))
        .RequireAuthorization();

        // PUT /places/businesses/{id} — update a business
        businesses.MapPut("/places/businesses/{id:guid}", async (
            Guid id,
            UpdateBusinessRequest request,
            ISender sender) =>
        {
            var result = await sender.Send(new UpdateBusinessCommand(
                Id: id,
                Name: request.Name,
                PlaceId: request.PlaceId,
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                Description: request.Description,
                Address: request.Address,
                City: request.City,
                Country: request.Country,
                Phone: request.Phone,
                Email: request.Email,
                Website: request.Website,
                IsHalal: request.IsHalal,
                HasVegetarianOptions: request.HasVegetarianOptions,
                HasAlcoholFreeArea: request.HasAlcoholFreeArea));
            return result.ToApiResult();
        })
        .WithName("UpdateBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Update a business (owner or admin only)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Update))
        .RequireAuthorization();

        // DELETE /places/businesses/{id} — soft-delete a business (admin only)
        businesses.MapDelete("/places/businesses/{id:guid}", async (
            Guid id,
            ISender sender) =>
        {
            var result = await sender.Send(new DeleteBusinessCommand(id));
            return result.ToApiResult();
        })
        .WithName("DeleteBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Soft-delete a business (admin only)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Delete))
        .RequireAuthorization("Admin");

        // POST /places/businesses/{id}/resubmit — owner resubmits a rejected business
        businesses.MapPost("/places/businesses/{id:guid}/resubmit", async (
            Guid id,
            ISender sender) =>
        {
            var result = await sender.Send(new ResubmitBusinessCommand(id));
            return result.ToApiResult();
        })
        .WithName("ResubmitBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Resubmit a rejected business for review (owner only; status must be Rejected)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Submit))
        .RequireAuthorization();

        // ── Admin transitions ──────────────────────────────────────────────────

        // POST /places/businesses/admin/{id}/approve
        businesses.MapPost("/places/businesses/admin/{id:guid}/approve", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender) =>
        {
            var result = await sender.Send(new ApproveBusinessCommand(id, currentUser.UserId!.Value));
            return result.ToApiResult();
        })
        .WithName("ApproveBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Approve a pending business (admin only; status must be Pending)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Approve))
        .RequireAuthorization("Admin");

        // POST /places/businesses/admin/{id}/reject
        businesses.MapPost("/places/businesses/admin/{id:guid}/reject", async (
            Guid id,
            RejectBusinessRequest request,
            ICurrentUser currentUser,
            ISender sender) =>
        {
            var result = await sender.Send(new RejectBusinessCommand(id, request.Reason, currentUser.UserId!.Value));
            return result.ToApiResult();
        })
        .WithName("RejectBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Reject a pending business with a reason (admin only; status must be Pending)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Reject))
        .RequireAuthorization("Admin");

        // POST /places/businesses/admin/{id}/request-more-docs
        businesses.MapPost("/places/businesses/admin/{id:guid}/request-more-docs", async (
            Guid id,
            RequestMoreDocsRequest request,
            ICurrentUser currentUser,
            ISender sender) =>
        {
            var result = await sender.Send(new RequestMoreDocsCommand(id, request.Reason, currentUser.UserId!.Value));
            return result.ToApiResult();
        })
        .WithName("RequestMoreBusinessDocs")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Request more documents from a pending business owner (admin only; status must be Pending or MoreDocsNeeded)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.RequestDocs))
        .RequireAuthorization("Admin");

        // POST /places/businesses/admin/{id}/suspend
        businesses.MapPost("/places/businesses/admin/{id:guid}/suspend", async (
            Guid id,
            SuspendBusinessRequest request,
            ICurrentUser currentUser,
            ISender sender) =>
        {
            var result = await sender.Send(new SuspendBusinessCommand(id, request.Reason, currentUser.UserId!.Value));
            return result.ToApiResult();
        })
        .WithName("SuspendBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Suspend an approved business with a reason (admin only; status must be Approved)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Suspend))
        .RequireAuthorization("Admin");

        // POST /places/businesses/admin/{id}/reinstate
        businesses.MapPost("/places/businesses/admin/{id:guid}/reinstate", async (
            Guid id,
            ICurrentUser currentUser,
            ISender sender) =>
        {
            var result = await sender.Send(new ReinstateBusinessCommand(id, currentUser.UserId!.Value));
            return result.ToApiResult();
        })
        .WithName("ReinstateBusiness")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .WithSummary("Reinstate a suspended business (admin only; status must be Suspended)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Reinstate))
        .RequireAuthorization("Admin");

        // ── Business Hours ─────────────────────────────────────────────────────

        // GET /places/businesses/{id}/hours
        businesses.MapGet("/places/businesses/{id:guid}/hours", async (
            Guid id,
            HttpContext http,
            ISender sender) =>
        {
            var (userId, isAdmin) = ExtractUser(http);
            var result = await sender.Send(
                new GetBusinessHoursQuery(id, userId, isAdmin),
                http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("GetBusinessHours")
        .Produces<IReadOnlyList<BusinessHoursDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Get operating hours for a business (public sees Approved only)")
        .AllowAnonymous();

        // PUT /places/businesses/{id}/hours
        businesses.MapPut("/places/businesses/{id:guid}/hours", async (
            Guid id,
            SetBusinessHoursRequest request,
            ISender sender) =>
        {
            var entries = request.Hours
                .Select(h => new BusinessHoursEntry(h.DayOfWeek, h.OpenTime, h.CloseTime, h.IsClosed))
                .ToList();

            var result = await sender.Send(new SetBusinessHoursCommand(id, entries));
            return result.ToApiResult();
        })
        .WithName("SetBusinessHours")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Replace all operating hours for a business (owner or admin only; max 2 entries/day)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.BusinessHours, AppAction.Update))
        .RequireAuthorization();

        // ── My Businesses ──────────────────────────────────────────────────────

        businesses.MapGet("/places/businesses/mine", async (
            HttpContext http,
            ISender sender,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(new GetMyBusinessesQuery(page, pageSize), http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("GetMyBusinesses")
        .Produces<IReadOnlyList<BusinessSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("List all businesses owned by the authenticated provider")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.Business, AppAction.Read))
        .RequireAuthorization();

        // ── Business Accessibility ─────────────────────────────────────────────

        businesses.MapGet("/places/businesses/{id:guid}/accessibility", async (
            Guid id,
            HttpContext http,
            ISender sender) =>
        {
            var result = await sender.Send(new GetBusinessAccessibilityFeaturesQuery(id), http.RequestAborted);
            return result.ToApiResult();
        })
        .WithName("GetBusinessAccessibilityFeatures")
        .Produces<IReadOnlyList<AccessibilityFeatureDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AllowAnonymous()
        .WithSummary("List accessibility features for a business");

        businesses.MapPut("/places/businesses/{id:guid}/accessibility", async (
            Guid id,
            IReadOnlyList<AccessibilityFeatureItemRequest> features,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateBusinessAccessibilityFeaturesCommand(id, features), ct);
            return result.ToApiResult();
        })
        .WithName("UpdateBusinessAccessibilityFeatures")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Replace all accessibility features for a business (owner only)")
        .WithMetadata(new MustHavePermissionAttribute(ContentPlacesFeatures.AccessibilityFeature, AppAction.Update))
        .RequireAuthorization();
    }

    internal static (Guid? UserId, bool IsAdmin) ExtractUser(HttpContext http)
    {
        var raw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = Guid.TryParse(raw, out var parsed) ? parsed : (Guid?)null;

        var roles = http.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value);

        var isAdmin = AppRoles.HighestPrivilegeLevel(roles) >= RolePrivilegeLevel.Admin;

        return (userId, isAdmin);
    }
}

using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.AccessibilityReviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

/// <summary>
/// Public-facing facade for accessibility reviews. <see cref="GetListAsync"/> is tolerant
/// (renders an empty section on failure rather than breaking the detail page), while the
/// mutating methods map backend failures to friendly messages. DISTINCT from
/// <see cref="ReviewsFacade"/> (normal reviews) — no RowVersion, bodyless DELETE.
/// </summary>
public sealed class AccessibilityReviewsFacade(AccessibilityReviewsApiClient api)
{
    private const int PageSize = 10;

    public async Task<AccessibilityReviewListVm> GetListAsync(
        string targetType, Guid targetId, int page = 1, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        try
        {
            var result = await api.GetForEntityAsync(targetType, targetId, pageNumber, PageSize, ct);
            var data = result is { IsSuccess: true, Data: { } pd } ? pd : null;

            return new AccessibilityReviewListVm
            {
                TargetType = targetType,
                TargetId = targetId,
                Items = data?.Items.Select(AccessibilityReviewMapper.ToRow).ToList() ?? [],
                TotalCount = data?.TotalCount ?? 0,
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new AccessibilityReviewListVm { TargetType = targetType, TargetId = targetId };
        }
    }

    /// <summary>The caller's own accessibility reviews, for the Accounts "My …" page.</summary>
    public async Task<ApiResult<MyAccessibilityReviewsVm>> GetMyAsync(CancellationToken ct = default)
    {
        var result = await api.GetMyAsync(cursor: null, pageSize: 50, ct);
        if (result.RequireSignOut) return ApiResult<MyAccessibilityReviewsVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<MyAccessibilityReviewsVm>.Fail(result.StatusCode, result.Error ?? "Could not load your accessibility reviews.");

        var rows = result.Data.Items
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MyAccessibilityReviewRowVm(
                r.Id,
                AccessibilityReviewMapper.TargetTypeName(r.TargetType),
                r.TargetId,
                r.Rating,
                r.Title,
                r.Content,
                AccessibilityReviewMapper.ParseFeatures(r.FeatureTypesCsv),
                r.CreatedAt,
                r.LastEditedAt))
            .ToList();

        return ApiResult<MyAccessibilityReviewsVm>.Ok(new MyAccessibilityReviewsVm { Items = rows });
    }

    public async Task<ApiResult> SubmitAsync(AccessibilityReviewFormVm vm, CancellationToken ct = default)
    {
        var body = new CreateAccessibilityReviewBody(
            AccessibilityReviewMapper.ParseTargetType(vm.TargetType),
            vm.TargetId,
            vm.Rating,
            string.IsNullOrWhiteSpace(vm.Title) ? null : vm.Title.Trim(),
            vm.Content.Trim(),
            vm.VisitDate,
            AccessibilityReviewMapper.ToCsv(vm.FeatureTypes));

        return Map(await api.CreateAsync(body, ct), "create", isEdit: false);
    }

    public async Task<ApiResult> EditAsync(Guid id, AccessibilityReviewFormVm vm, CancellationToken ct = default)
    {
        var body = new UpdateAccessibilityReviewBody(
            vm.Rating,
            string.IsNullOrWhiteSpace(vm.Title) ? null : vm.Title.Trim(),
            vm.Content.Trim(),
            vm.VisitDate,
            AccessibilityReviewMapper.ToCsv(vm.FeatureTypes));

        return Map(await api.EditAsync(id, body, ct), "edit", isEdit: true);
    }

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Map(await api.DeleteAsync(id, ct), "delete", isEdit: false);

    private static ApiResult Map(ApiResult result, string verb, bool isEdit)
    {
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.RequireSignOut) return ApiResult.ForceSignOut();

        // A 400/422 WITH a field-error dictionary is genuine input validation — surface it.
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);

        var message = result.StatusCode switch
        {
            403 => "You can only edit or delete your own accessibility review.",
            404 => "That accessibility review could not be found.",
            409 => "You have already submitted an accessibility review for this entity.",
            // A non-field 400/422 on the edit path is the 48-hour edit-window guard. The
            // non-generic ApiResult pipeline drops the backend detail on a 400, so we can't
            // read the message — but EditWindowExpired is the only such case for an edit.
            400 or 422 when isEdit
                => "Accessibility reviews can only be edited within 48 hours of submission.",
            400 or 422 => result.Error is { Length: > 0 } e ? e : $"Could not {verb} the accessibility review.",
            _ => result.Error ?? $"Could not {verb} the accessibility review.",
        };
        return ApiResult.Fail(result.StatusCode, message);
    }
}

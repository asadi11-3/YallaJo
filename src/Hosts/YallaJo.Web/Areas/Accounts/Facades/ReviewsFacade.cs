using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class ReviewsFacade
{
    private const int PageSize = 50;

    private readonly ReviewsApiClient _api;
    private readonly ILogger<ReviewsFacade> _logger;

    public ReviewsFacade(ReviewsApiClient api, ILogger<ReviewsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<ReviewsVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyReviewsAsync(pageSize: PageSize, ct: ct);

        if (result.IsUnauthorized)
            return ApiResult<ReviewsVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ReviewsVm>.Fail(result.StatusCode, result.Error ?? "Could not load your reviews.");

        var rows = result.Data.Items
            .Select(ToRow)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return ApiResult<ReviewsVm>.Ok(new ReviewsVm { Reviews = rows });
    }

    public async Task<ApiResult> EditAsync(EditReviewFormVm form, CancellationToken ct = default)
    {
        var request = new EditReviewRequest(
            form.Rating,
            string.IsNullOrWhiteSpace(form.Title) ? null : form.Title.Trim(),
            form.Content.Trim(),
            form.VisitDate,
            string.IsNullOrWhiteSpace(form.RowVersion) ? null : form.RowVersion);

        try
        {
            var result = await _api.EditAsync(form.Id, request, ct);
            return Normalize(result, "Could not save your changes.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to edit review {ReviewId}", form.Id);
            return ApiResult.Fail("Could not save your changes.");
        }
    }

    public async Task<ApiResult> DeleteAsync(Guid id, string? rowVersion, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.DeleteAsync(
                id,
                new DeleteReviewRequest(string.IsNullOrWhiteSpace(rowVersion) ? null : rowVersion),
                ct);
            // DELETE is idempotent; treat NotFound as already gone.
            if (result.IsSuccess || result.IsNotFound)
                return ApiResult.Ok();
            return Normalize(result, "Could not delete the review.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete review {ReviewId}", id);
            return ApiResult.Fail("Could not delete the review.");
        }
    }

    public async Task<ApiResult> MarkHelpfulAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var result = await _api.MarkHelpfulAsync(id, ct);
            return Normalize(result, "Could not record your vote.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to mark review {ReviewId} helpful", id);
            return ApiResult.Fail("Could not record your vote.");
        }
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static ReviewRowVm ToRow(ReviewItemResponse r)
    {
        var (label, url) = TargetInfo(r.TargetType, r.TargetId);
        return new ReviewRowVm(
            r.Id,
            r.TargetType,
            r.TargetId,
            label,
            url,
            r.Rating,
            r.Title,
            r.Content,
            r.VisitDate,
            r.Status,
            r.IsVerifiedBooking,
            r.HelpfulVoteCount,
            r.LastEditedAt,
            r.CreatedAt,
            r.RowVersion,
            r.Replies.Select(x => new ReviewReplyRowVm(x.Id, x.Content, x.CreatedAt, x.LastEditedAt)).ToList());
    }

    private static (string Label, string? Url) TargetInfo(string targetType, Guid targetId) => targetType switch
    {
        "Tour" => ("Tour", $"/tours/{targetId}"),
        "Place" => ("Place", $"/places/{targetId}"),
        "Business" => ("Business", $"/places/businesses/{targetId}"),
        "TourGuide" => ("Tour guide", $"/guides/{targetId}"),
        _ => (targetType, null),
    };
}

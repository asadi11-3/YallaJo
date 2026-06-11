using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>Builds the Reviews view model and posts review replies (B3) for the logged-in guide.</summary>
public sealed class GuideReviewsFacade
{
    private const int DefaultPageSize = 20;
    private const int MaxReplyLength = 2000;

    private readonly ReviewsApiClient _api;
    private readonly ILogger<GuideReviewsFacade> _logger;

    public GuideReviewsFacade(ReviewsApiClient api, ILogger<GuideReviewsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<ReviewsVm>> GetAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = DefaultPageSize;

        var profileResult = await _api.GetMyProfileAsync(ct);
        if (profileResult.RequireSignOut) return ApiResult<ReviewsVm>.ForceSignOut();
        if (!profileResult.IsSuccess || profileResult.Data is null)
            return ApiResult<ReviewsVm>.Fail(profileResult.StatusCode, profileResult.Error);

        var guideId = profileResult.Data.Id;

        var reviewsTask = SafeReviewsAsync(guideId, page, pageSize, ct);
        var summaryTask = SafeSummaryAsync(guideId, ct);
        await Task.WhenAll(reviewsTask, summaryTask);

        var reviews = await reviewsTask; // UI-PERF-R1: no .Result
        var summary = await summaryTask;

        var rows = reviews.Items
            .Select(r => new ReviewRowVm(
                r.Id,
                r.Rating,
                r.Title,
                r.Content,
                r.Status,
                r.IsVerifiedBooking,
                r.HelpfulVoteCount,
                r.VisitDate,
                r.CreatedAt,
                (r.Replies ?? [])
                    .Select(rep => new ReviewReplyRowVm(rep.Id, rep.Content, rep.CreatedAt))
                    .ToList()))
            .ToList();

        var vm = new ReviewsVm
        {
            AverageRating = summary?.AverageRating ?? 0m,
            ReviewCount = summary?.ReviewCount ?? reviews.TotalCount,
            Reviews = rows,
            Page = page,
            PageSize = pageSize,
            TotalCount = reviews.TotalCount,
        };

        return ApiResult<ReviewsVm>.Ok(vm);
    }

    /// <summary>Posts the guide's reply to a review (B3). Content is trimmed and capped at 2000 chars.</summary>
    public async Task<ApiResult> ReplyAsync(Guid reviewId, string? content, CancellationToken ct = default)
    {
        var trimmed = content?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return ApiResult.Fail(400, "Please write a reply before posting.");
        }

        if (trimmed.Length > MaxReplyLength)
        {
            trimmed = trimmed[..MaxReplyLength];
        }

        return await _api.AddReplyAsync(reviewId, trimmed, ct);
    }

    private async Task<PublicReviewPageResponse> SafeReviewsAsync(Guid guideId, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetReviewsAsync(guideId, page, pageSize, ct);
            return result is { IsSuccess: true, Data: not null }
                ? result.Data
                : new PublicReviewPageResponse([], page, pageSize, 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load reviews for guide {GuideId}.", guideId);
            return new PublicReviewPageResponse([], page, pageSize, 0);
        }
    }

    private async Task<RatingSummaryResponse?> SafeSummaryAsync(Guid guideId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetRatingSummaryAsync(guideId, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load rating summary for guide {GuideId}.", guideId);
            return null;
        }
    }
}

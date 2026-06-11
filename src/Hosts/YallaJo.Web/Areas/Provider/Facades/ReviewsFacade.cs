using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class ReviewsFacade
{
    private const int PageSize = 50;

    private readonly ReviewsApiClient _api;

    public ReviewsFacade(ReviewsApiClient api) => _api = api;

    public async Task<ApiResult<ReviewsVm>> GetAsync(Guid? selectedTourId, CancellationToken ct = default)
    {
        var toursResult = await _api.GetMyToursAsync(ct);
        if (toursResult.IsUnauthorized)
            return ApiResult<ReviewsVm>.ForceSignOut();

        var tours = toursResult is { IsSuccess: true, Data: not null }
            ? toursResult.Data.Items
                .Select(t => new TourOptionVm { Id = t.Id, Name = t.Name })
                .ToList()
            : [];

        if (tours.Count == 0)
            return ApiResult<ReviewsVm>.Ok(new ReviewsVm());

        // Default to the first tour when none selected (or selection is unknown).
        var current = tours.FirstOrDefault(t => t.Id == selectedTourId) ?? tours[0];

        var reviews = new List<ReviewRowVm>();
        decimal averageRating = 0m;
        var reviewCount = 0;

        try
        {
            // [Backend] B6 / API7: one grouped ratings call for ALL tour options
            // (replaces the per-tour GetRatingAsync) in parallel with the review page.
            var reviewsTask = _api.GetReviewsAsync(current.Id, 1, PageSize, ct);
            var ratingsBatchTask = _api.GetRatingsBatchAsync(tours.Select(t => t.Id), ct);
            await Task.WhenAll(reviewsTask, ratingsBatchTask);

            if (reviewsTask.Result is { IsSuccess: true, Data: not null } rr)
            {
                reviews = rr.Data.Items
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewRowVm
                    {
                        Id = r.Id,
                        Rating = r.Rating,
                        Title = r.Title,
                        Content = r.Content,
                        IsVerifiedBooking = r.IsVerifiedBooking,
                        HelpfulVoteCount = r.HelpfulVoteCount,
                        CreatedAt = r.CreatedAt,
                        ReplyContent = r.Replies
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefault()?.Content,
                        ReplyId = r.Replies
                            .OrderByDescending(p => p.CreatedAt)
                            .FirstOrDefault()?.Id,
                    })
                    .ToList();
            }

            if (ratingsBatchTask.Result is { IsSuccess: true, Data: not null } batch)
            {
                // Enrich every selector option with its rating and resolve the
                // currently-selected tour's aggregate from the same response.
                var byId = batch.Data.ToDictionary(i => i.EntityId);
                foreach (var option in tours)
                {
                    if (byId.TryGetValue(option.Id, out var item))
                    {
                        option.AverageRating = item.Average;
                        option.ReviewCount = item.Count;
                    }
                }

                if (byId.TryGetValue(current.Id, out var selected))
                {
                    averageRating = selected.Average;
                    reviewCount = selected.Count;
                }
            }
        }
        catch
        {
            // tolerate partial failure — show the selector with empty reviews
        }

        return ApiResult<ReviewsVm>.Ok(new ReviewsVm
        {
            Tours = tours,
            SelectedTourId = current.Id,
            SelectedTourName = current.Name,
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            Reviews = reviews,
        });
    }

    public async Task<ApiResult> ReplyAsync(Guid reviewId, string content, CancellationToken ct = default)
    {
        var result = await _api.ReplyAsync(reviewId, new AddReplyRequest(content.Trim()), ct);
        return Normalize(result, "Your reply could not be saved.");
    }

    public async Task<ApiResult> EditReplyAsync(Guid reviewId, Guid replyId, string content, CancellationToken ct = default)
    {
        var result = await _api.UpdateReplyAsync(reviewId, replyId, new AddReplyRequest(content.Trim()), ct);
        return Normalize(result, "Your reply could not be updated.");
    }

    public async Task<ApiResult> DeleteReplyAsync(Guid reviewId, Guid replyId, CancellationToken ct = default)
    {
        var result = await _api.DeleteReplyAsync(reviewId, replyId, ct);
        return Normalize(result, "Your reply could not be deleted.");
    }

    public async Task<ApiResult> ReportAsync(Guid reviewId, string reason, string? description, CancellationToken ct = default)
    {
        var request = new ReviewReportRequest(
            string.IsNullOrWhiteSpace(reason) ? "Other" : reason.Trim(),
            (description ?? string.Empty).Trim());
        var result = await _api.ReportAsync(reviewId, request, ct);
        return Normalize(result, "Your report could not be submitted.");
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsSuccess)
            return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}

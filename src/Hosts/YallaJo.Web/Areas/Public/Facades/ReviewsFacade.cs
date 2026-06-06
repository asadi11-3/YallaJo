using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class ReviewsFacade(ReviewsApiClient api)
{
    private const int PageSize = 10;

    public Task<ApiResult> SubmitReviewAsync(ReviewFormVm vm, CancellationToken ct = default)
    {
        var body = new CreateReviewBody(
            vm.TargetType,
            vm.TargetId,
            vm.Rating,
            string.IsNullOrWhiteSpace(vm.Title) ? null : vm.Title.Trim(),
            vm.Content.Trim(),
            vm.VisitDate);
        return api.CreateReviewAsync(body, ct);
    }

    public Task<ApiResult> EditReviewAsync(ReviewEditFormVm vm, CancellationToken ct = default)
    {
        var body = new EditReviewBody(
            vm.Rating,
            string.IsNullOrWhiteSpace(vm.Title) ? null : vm.Title.Trim(),
            vm.Content.Trim(),
            vm.VisitDate,
            vm.RowVersion);
        return api.EditReviewAsync(vm.ReviewId, body, ct);
    }

    public Task<ApiResult> DeleteReviewAsync(Guid reviewId, string? rowVersion, CancellationToken ct = default)
        => api.DeleteReviewAsync(reviewId, new DeleteReviewBody(rowVersion), ct);

    public Task<ApiResult> SubmitReportAsync(ReportFormVm vm, CancellationToken ct = default)
    {
        var body = new SubmitReportBody(vm.EntityType, vm.EntityId, vm.Reason, vm.Description.Trim());
        return api.ReportEntityAsync(body, ct);
    }

    public Task<ApiResult> ReportReviewAsync(Guid reviewId, ReportFormVm vm, CancellationToken ct = default)
    {
        var body = new ReviewReportBody(vm.Reason, vm.Description.Trim());
        return api.ReportReviewAsync(reviewId, body, ct);
    }

    public Task<ApiResult> MarkHelpfulAsync(Guid reviewId, CancellationToken ct = default)
        => api.MarkHelpfulAsync(reviewId, ct);

    public Task<ApiResult> UnmarkHelpfulAsync(Guid reviewId, CancellationToken ct = default)
        => api.UnmarkHelpfulAsync(reviewId, ct);

    public async Task<ReviewListVm> GetReviewListAsync(string targetType, Guid targetId, int page = 1, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        try
        {
            var reviewsTask = api.GetReviewsAsync(targetType, targetId, pageNumber, PageSize, ct);
            var ratingsTask = api.GetRatingsAsync(targetType, targetId, ct);
            await Task.WhenAll(reviewsTask, ratingsTask);

            var pageData = reviewsTask.Result is { IsSuccess: true, Data: { } pd } ? pd : null;
            var ratings = ratingsTask.Result is { IsSuccess: true, Data: { } rs } ? rs : null;

            var items = pageData?.Items.Select(ReviewMapper.ToItem).ToList() ?? [];

            return new ReviewListVm
            {
                TargetType = targetType,
                TargetId = targetId,
                Items = items,
                AverageRating = ratings?.AverageRating ?? 0m,
                ReviewCount = ratings?.ReviewCount ?? (pageData?.TotalCount ?? 0),
                Page = pageData?.Page ?? pageNumber,
                PageSize = pageData?.PageSize ?? PageSize,
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return new ReviewListVm { TargetType = targetType, TargetId = targetId };
        }
    }
}

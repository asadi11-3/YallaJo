using Microsoft.AspNetCore.Http;
using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Reviews;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

/// <summary>Outcome of a review submission that may also carry image uploads.</summary>
public sealed record SubmitReviewResult(ApiResult Result, bool AnyImageFailed)
{
    public bool IsSuccess => Result.IsSuccess;
}

public sealed class ReviewsFacade(ReviewsApiClient api, IApiAssetUrlResolver assetResolver)
{
    private const int PageSize = 10;

    public Task<ApiResult> SubmitReviewAsync(ReviewFormVm vm, CancellationToken ct = default)
    {
        var body = BuildCreateBody(vm);
        return api.CreateReviewAsync(body, ct);
    }

    /// <summary>
    /// Creates the review, then (best-effort) uploads any selected images via the ContentCore
    /// attachment pipeline. If the review is created but an image upload fails, the review is NOT
    /// rolled back — AnyImageFailed is set so the caller can surface a warning.
    /// </summary>
    public async Task<SubmitReviewResult> SubmitReviewWithImagesAsync(
        ReviewFormVm vm,
        IReadOnlyList<IFormFile> files,
        CancellationToken ct = default)
    {
        var body = BuildCreateBody(vm);

        // No images: behave exactly like the plain create flow (return a non-generic ApiResult).
        if (files.Count == 0)
        {
            var plain = await api.CreateReviewAsync(body, ct);
            return new SubmitReviewResult(plain, AnyImageFailed: false);
        }

        var created = await api.CreateReviewWithIdAsync(body, ct);
        if (!created.IsSuccess)
        {
            return new SubmitReviewResult(
                ApiResult.Fail(created.StatusCode, created.Error),
                AnyImageFailed: false);
        }

        var reviewId = created.Data;
        var anyImageFailed = false;
        foreach (var file in files)
        {
            if (file is null || file.Length == 0)
            {
                continue;
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var upload = await api.UploadImageAsync(reviewId, stream, file.FileName, file.ContentType, ct);
                if (!upload.IsSuccess)
                {
                    anyImageFailed = true;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                anyImageFailed = true;
            }
        }

        // Review was created successfully regardless of image outcome — never roll back.
        return new SubmitReviewResult(ApiResult.Ok(created.StatusCode), anyImageFailed);
    }

    private static CreateReviewBody BuildCreateBody(ReviewFormVm vm) => new(
        vm.TargetType,
        vm.TargetId,
        vm.Rating,
        string.IsNullOrWhiteSpace(vm.Title) ? null : vm.Title.Trim(),
        vm.Content.Trim(),
        vm.VisitDate);

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

            var items = pageData?.Items.Select(r => ReviewMapper.ToItem(r, assetResolver)).ToList() ?? [];

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

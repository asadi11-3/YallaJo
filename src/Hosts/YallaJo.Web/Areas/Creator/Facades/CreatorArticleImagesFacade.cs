using Microsoft.AspNetCore.Http;
using YallaJo.Web.Areas.Creator.ApiClients;
using YallaJo.Web.Areas.Creator.Models.Articles.Images;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Creator.Facades;

public sealed class CreatorArticleImagesFacade
{
    private readonly CreatorArticleImagesApiClient _images;

    public CreatorArticleImagesFacade(CreatorArticleImagesApiClient images) => _images = images;

    /// <summary>Loads the image section VM for a Blog article.</summary>
    public async Task<ApiResult<ArticleImagesVm>> GetImagesAsync(Guid blogId, CancellationToken ct = default)
    {
        var result = await _images.ListAsync(blogId, ct).ConfigureAwait(false);

        if (result.RequireSignOut) return ApiResult<ArticleImagesVm>.ForceSignOut();
        if (result.IsForbidden)
            return ApiResult<ArticleImagesVm>.Fail(403, "You don't have permission to view these images.");
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ArticleImagesVm>.Fail(result.StatusCode, result.Error ?? "Could not load images.");

        return ApiResult<ArticleImagesVm>.Ok(ArticleImagesMapper.ToVm(blogId, result.Data));
    }

    public async Task<ApiResult> UploadAsync(
        Guid blogId, IReadOnlyList<IFormFile> files, int existingCount, CancellationToken ct = default)
    {
        if (files.Count == 0)
            return ApiResult.Fail(400, "Please choose at least one image to upload.");

        var remaining = ArticleImagesMapper.MaxImagesPerBlog - existingCount;
        if (remaining <= 0)
            return ApiResult.Fail(422, $"This article already has the maximum of {ArticleImagesMapper.MaxImagesPerBlog} images.");

        if (files.Count > remaining)
            return ApiResult.Fail(422,
                $"You can add {remaining} more image(s); you selected {files.Count}.");

        var errors = new List<string>();
        var uploaded = 0;
        var sortOrder = existingCount;

        foreach (var file in files)
        {
            var validationError = ArticleImagesMapper.ValidateImage(file.FileName, file.ContentType, file.Length);
            if (validationError is not null)
            {
                errors.Add(validationError);
                continue;
            }

            await using var stream = file.OpenReadStream();
            var result = await _images
                .UploadAsync(blogId, stream, file.FileName, file.ContentType, sortOrder, ct)
                .ConfigureAwait(false);

            if (result.RequireSignOut) return ApiResult.ForceSignOut();

            if (result.IsSuccess)
            {
                uploaded++;
                sortOrder++;
            }
            else if (result.IsForbidden)
            {
                return ApiResult.Fail(403, "You don't have permission to upload images to this article.");
            }
            else
            {
                errors.Add($"\"{file.FileName}\": {result.Error ?? "upload failed"}");
            }
        }

        if (uploaded == 0)
            return ApiResult.Fail(422, errors.Count > 0
                ? "No images were uploaded. " + string.Join(" ", errors)
                : "No images were uploaded.");

        if (errors.Count > 0)
            return ApiResult.Fail(207, $"Uploaded {uploaded} image(s); some failed: {string.Join(" ", errors)}");

        return ApiResult.Ok(200);
    }

    public async Task<ApiResult> DeleteAsync(Guid attachmentId, CancellationToken ct = default)
        => Normalize(await _images.DeleteAsync(attachmentId, ct).ConfigureAwait(false), "delete this image");

    public async Task<ApiResult> SetPrimaryAsync(Guid blogId, Guid attachmentId, CancellationToken ct = default)
        => Normalize(await _images.SetPrimaryAsync(blogId, attachmentId, ct).ConfigureAwait(false), "set the primary image");

    public async Task<ApiResult> ReorderAsync(
        Guid blogId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default)
        => Normalize(await _images.ReorderAsync(blogId, orderedIds, ct).ConfigureAwait(false), "reorder the images");

    private static ApiResult Normalize(ApiResult result, string verb)
    {
        if (result.RequireSignOut) return ApiResult.ForceSignOut();
        if (result.IsSuccess) return ApiResult.Ok(result.StatusCode);
        return ApiResult.Fail(result.StatusCode, result.Error ?? FriendlyError(result.StatusCode, verb));
    }

    private static string FriendlyError(int statusCode, string verb) => statusCode switch
    {
        403 => "You don't have permission to perform this action.",
        404 => "We couldn't find that image.",
        409 => "This image was modified elsewhere. Please refresh and try again.",
        _   => $"Could not {verb}. Please try again.",
    };
}

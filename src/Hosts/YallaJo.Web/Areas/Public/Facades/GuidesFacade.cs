using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Guides;
using YallaJo.Web.Areas.Public.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Seo;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class GuidesFacade(GuidesApiClient api, SeoApiClient seo, TranslationsApiClient translations, IApiAssetUrlResolver assetResolver)
{
    private const int PageSize = 12;

    public async Task<ApiResult<GuidesGridVm>> GetGridAsync(int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var result = await api.GetGuidesAsync(pageNumber, PageSize, ct);
        if (result is not { IsSuccess: true, Data: { } data })
        {
            return ApiResult<GuidesGridVm>.Fail(result.StatusCode, result.Error ?? "Could not load guides.");
        }

        var cards = data.Items.Select(g => new GuideCardVm
        {
            Id = g.Id,
            Slug = g.Slug,
            DisplayName = string.IsNullOrWhiteSpace(g.DisplayName) ? "Guide" : g.DisplayName,
            AvatarUrl = assetResolver.Resolve(g.AvatarUrl),
            Bio = g.Bio,
            AverageRating = g.AverageRating,
            ReviewCount = g.ReviewCount,
            TourCount = g.TourCount,
            LanguageCount = g.Languages.Count,
            SpecializationCount = g.Specializations.Count,
        }).ToList();

        return ApiResult<GuidesGridVm>.Ok(new GuidesGridVm
        {
            Guides = cards,
            PageNumber = data.Page,
            PageSize = data.PageSize,
            TotalCount = data.TotalCount,
            HasPreviousPage = data.Page > 1,
            HasNextPage = (long)data.Page * data.PageSize < data.TotalCount,
        });
    }

    public async Task<ApiResult<GuideDetailVm>> GetDetailAsync(string slug, CancellationToken ct = default)
    {
        var result = await api.GetGuideBySlugAsync(slug, ct);
        if (result is not { IsSuccess: true, Data: { } d })
        {
            return ApiResult<GuideDetailVm>.Fail(result.StatusCode, result.Error ?? "Guide not found.");
        }

        var avatar = assetResolver.Resolve(d.AvatarUrl);
        var languageCode = TranslationOverlay.ActiveLanguageCode;
        var guideTranslations = await TranslationOverlay.GetApprovedAsync(translations, "TourGuide", d.Id, languageCode, ct);
        var displayName = string.IsNullOrWhiteSpace(d.DisplayName) ? "Guide" : d.DisplayName;
        var vm = new GuideDetailVm
        {
            Id = d.Id,
            Slug = slug,
            DisplayName = TranslationOverlay.Apply(guideTranslations, "DisplayName", displayName, languageCode) ?? displayName,
            AvatarUrl = avatar,
            Bio = TranslationOverlay.Apply(guideTranslations, "Bio", d.Bio, languageCode),
            YearsOfExperience = d.YearsOfExperience,
            HasFirstAid = d.HasFirstAid,
            AverageRating = d.AverageRating,
            ReviewCount = d.ReviewCount,
            TourCount = d.TourCount,
            LanguageCount = d.Languages.Count,
            SpecializationCount = d.Specializations.Count,
        };

        vm.Tours = await SafeGuideToursAsync(d.Id, ct);
        vm.Seo = TranslationOverlay.ApplySeo(
            await GetSeoAsync(d.Id, d.Bio, avatar, ct), guideTranslations, languageCode,
            fallbackTitle: vm.DisplayName,
            fallbackDescription: vm.Bio);
        return ApiResult<GuideDetailVm>.Ok(vm);
    }

    private async Task<IReadOnlyList<GuideTourLinkVm>> SafeGuideToursAsync(Guid guideId, CancellationToken ct)
    {
        try
        {
            var result = await api.GetGuideToursAsync(guideId, 1, PageSize, ct);
            if (result is { IsSuccess: true, Data: { } tours })
            {
                return tours.Items.Select(t => new GuideTourLinkVm
                {
                    TourId = t.TourId,
                    Title = t.Title,
                    Slug = t.Slug,
                    OffersPrivateTour = t.OffersPrivateTour,
                }).ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // tolerate - the tours rail is best-effort and never breaks the page
        }

        return [];
    }

    private async Task<SeoContent> GetSeoAsync(Guid guideId, string? bio, string? avatar, CancellationToken ct)
    {
        string? metaTitle = null;
        var metaDescription = bio;
        string? canonical = null;
        var ogImage = avatar;
        IReadOnlyList<SeoFaqItem> faqs = [];

        try
        {
            var metaTask = seo.GetMetadataAsync(SeoEntityType.TourGuide, guideId, ct);
            var faqTask = seo.GetFaqAsync(SeoEntityType.TourGuide, guideId, ct);
            await Task.WhenAll(metaTask, faqTask);

            if (metaTask.Result is { IsSuccess: true, Data: { } meta })
            {
                if (!string.IsNullOrWhiteSpace(meta.Title)) metaTitle = meta.Title;
                if (!string.IsNullOrWhiteSpace(meta.Description)) metaDescription = meta.Description;
                canonical = meta.Canonical;
                if (!string.IsNullOrWhiteSpace(meta.OgImage)) ogImage = meta.OgImage;
            }

            if (faqTask.Result is { IsSuccess: true, Data: { } items })
            {
                faqs = items.OrderBy(f => f.SortOrder)
                    .Select(f => new SeoFaqItem { Question = f.Question, Answer = f.Answer })
                    .ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // tolerate - SEO enrichment is best-effort
        }

        return new SeoContent
        {
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            Canonical = canonical,
            OgImage = ogImage,
            Faqs = faqs,
        };
    }
}

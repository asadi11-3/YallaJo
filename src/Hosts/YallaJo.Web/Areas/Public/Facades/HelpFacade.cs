using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Help;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class HelpFacade
{
    private const int FetchPageSize = 100;

    private readonly HelpApiClient _api;

    public HelpFacade(HelpApiClient api) => _api = api;

    public async Task<ApiResult<HelpVm>> GetHelpAsync(CancellationToken ct = default)
    {
        var faqs = await LoadFaqsAsync(ct);
        return ApiResult<HelpVm>.Ok(new HelpVm { Faqs = faqs });
    }

    public async Task<ApiResult<HelpDetailVm>> GetFaqAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.GetFaqsAsync(1, FetchPageSize, ct);
        if (result.IsUnauthorized)
            return ApiResult<HelpDetailVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<HelpDetailVm>.Fail(result.StatusCode, result.Error ?? "Could not load help content.");

        var match = result.Data.Items.FirstOrDefault(f => f.Id == id);
        if (match is null)
            return ApiResult<HelpDetailVm>.Fail(404, "Article not found.");

        return ApiResult<HelpDetailVm>.Ok(new HelpDetailVm
        {
            Id = match.Id,
            Question = match.Question,
            Answer = match.Answer
        });
    }

    private async Task<IReadOnlyList<FaqVm>> LoadFaqsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetFaqsAsync(1, FetchPageSize, ct);
            if (!result.IsSuccess || result.Data is null)
                return [];

            return result.Data.Items
                .OrderBy(f => f.SortOrder)
                .Select(f => new FaqVm { Id = f.Id, Question = f.Question, Answer = f.Answer })
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}

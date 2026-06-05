// <copyright file="SeoFaqApiClient.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.ApiClients;

using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Admin.Models.SeoFaq;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

public sealed class SeoFaqApiClient
{
    private const string Base = "/api/v1/seo/faq";

    private readonly IApiClient _api;

    public SeoFaqApiClient(IApiClient api) => this._api = api;

    public Task<ApiResult<PaginatedFaqResponse>> GetFaqItemsAsync(FaqFilterRequest filter, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = filter.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = filter.PageSize.ToString(CultureInfo.InvariantCulture),
        };

        if (filter.EntityType.HasValue)
        {
            query["entityType"] = filter.EntityType.Value.ToString();
        }

        if (filter.ActiveOnly.HasValue)
        {
            query["activeOnly"] = filter.ActiveOnly.Value ? "true" : "false";
        }

        var url = QueryHelpers.AddQueryString(Base, query);
        return this._api.GetAsync<PaginatedFaqResponse>(url, ct);
    }

    public Task<ApiResult<CreateFaqItemResponse>> CreateAsync(CreateFaqItemApiRequest request, CancellationToken ct = default)
        => this._api.PostAsync<CreateFaqItemResponse>(Base, request, ct);

    public Task<ApiResult> UpdateAsync(Guid id, UpdateFaqItemApiRequest request, CancellationToken ct = default)
        => this._api.PutAsync($"{Base}/{id:D}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => this._api.DeleteAsync($"{Base}/{id:D}", ct);
}

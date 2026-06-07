// <copyright file="SeoFaqFacade.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Facades;

using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.SeoFaq;
using YallaJo.Web.Infrastructure.Api.Contracts;

public sealed class SeoFaqFacade
{
    private readonly SeoFaqApiClient _api;

    public SeoFaqFacade(SeoFaqApiClient api) => this._api = api;

    public async Task<ApiResult<SeoFaqVm>> GetIndexAsync(FaqFilterRequest filter, CancellationToken ct = default)
    {
        if (filter.PageSize is < 1 or > 200)
        {
            filter.PageSize = 50;
        }

        if (filter.Page < 1)
        {
            filter.Page = 1;
        }

        var result = await this._api.GetFaqItemsAsync(filter, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult<SeoFaqVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<SeoFaqVm>.Fail(result.StatusCode, result.Error ?? "Could not load FAQ items.");
        }

        return ApiResult<SeoFaqVm>.Ok(SeoFaqMapper.ToVm(result.Data, filter));
    }

    public Task<ApiResult> CreateAsync(CreateFaqItemFormVm form, CancellationToken ct = default)
    {
        var request = new CreateFaqItemApiRequest(
            form.EntityType,
            form.EntityId,
            form.Question.Trim(),
            form.Answer.Trim(),
            form.SortOrder);
        return Normalize(this._api.CreateAsync(request, ct), "Could not create the FAQ item.");
    }

    public Task<ApiResult> UpdateAsync(UpdateFaqItemFormVm form, CancellationToken ct = default)
    {
        var request = new UpdateFaqItemApiRequest(form.Question.Trim(), form.Answer.Trim());
        return Normalize(this._api.UpdateAsync(form.Id, request, ct), "Could not update the FAQ item.");
    }

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => Normalize(this._api.DeleteAsync(id, ct), "Could not delete the FAQ item.");

    // §8.10 — batch reorder FAQ items within one entity's list.
    public Task<ApiResult> ReorderAsync(
        SeoEntityType entityType, Guid entityId, IReadOnlyList<ReorderFaqItemApi> items, CancellationToken ct = default)
    {
        if (entityId == Guid.Empty)
        {
            return Task.FromResult(ApiResult.Fail(400, "A valid entity is required to reorder FAQ items."));
        }

        if (items is null || items.Count == 0)
        {
            return Task.FromResult(ApiResult.Fail(400, "No FAQ items were provided to reorder."));
        }

        var request = new ReorderFaqItemsApiRequest(entityType, entityId, items);
        return Normalize(this._api.ReorderAsync(request, ct), "Could not reorder the FAQ items.");
    }

    private static async Task<ApiResult> Normalize<T>(Task<ApiResult<T>> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "The FAQ item was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action conflicts with the current state. Please reload and try again.");
        }

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.Invalid(result.ValidationErrors);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "The FAQ item was not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This action conflicts with the current state. Please reload and try again.");
        }

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.Invalid(result.ValidationErrors);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}

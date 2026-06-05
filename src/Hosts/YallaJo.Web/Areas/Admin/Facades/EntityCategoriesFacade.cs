using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Categories;
using YallaJo.Web.Areas.Admin.Models.EntityCategories;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class EntityCategoriesFacade
{
    private readonly EntityCategoriesApiClient _entityCategories;
    private readonly CategoriesApiClient _categories;
    private readonly ILogger<EntityCategoriesFacade> _logger;

    public EntityCategoriesFacade(
        EntityCategoriesApiClient entityCategories,
        CategoriesApiClient categories,
        ILogger<EntityCategoriesFacade> logger)
    {
        _entityCategories = entityCategories;
        _categories = categories;
        _logger = logger;
    }

    public async Task<ApiResult<EntityCategoriesVm>> GetAsync(
        string? entityType, Guid? entityId, CancellationToken ct = default)
    {
        var options = await SafeAvailableCategoriesAsync(ct);
        var vm = new EntityCategoriesVm
        {
            AvailableCategories = options,
            Query = new EntityCategoriesQueryVm
            {
                EntityType = entityType ?? "",
                EntityId = entityId ?? Guid.Empty
            }
        };

        if (string.IsNullOrWhiteSpace(entityType) || entityId is null || entityId == Guid.Empty)
            return ApiResult<EntityCategoriesVm>.CreateSuccess(vm);

        var assigned = await _entityCategories.GetAssignedAsync(entityType, entityId.Value, ct);
        if (assigned.RequireSignOut)
            return ApiResult<EntityCategoriesVm>.ForceSignOut();
        if (!assigned.IsSuccess || assigned.Data is null)
            return ApiResult<EntityCategoriesVm>.CreateFailure(assigned.StatusCode, assigned.Error);

        vm.HasQueried = true;
        vm.Assigned = assigned.Data.Select(x => x.ToRow()).ToList();
        return ApiResult<EntityCategoriesVm>.CreateSuccess(vm);
    }

    public async Task<ApiResult> AssignAsync(
        string entityType, Guid entityId, IReadOnlyList<Guid> categoryIds, CancellationToken ct = default)
    {
        if (categoryIds.Count == 0)
            return ApiResult.Invalid(new Dictionary<string, string[]>
            {
                ["CategoryIds"] = new[] { "Select at least one category to assign." }
            });

        try
        {
            var req = new AssignCategoriesToEntityRequest(entityType.Trim(), entityId, categoryIds);
            var r = await _entityCategories.AssignAsync(req, ct);
            return Normalize(r, "Could not assign categories.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to assign categories to entity {EntityType}/{EntityId}", entityType, entityId);
            return ApiResult.Fail("Could not assign categories.");
        }
    }

    public async Task<ApiResult> RemoveAsync(
        string entityType, Guid entityId, Guid categoryId, CancellationToken ct = default)
    {
        try
        {
            var r = await _entityCategories.RemoveAsync(entityType, entityId, categoryId, ct);
            return Normalize(r, "Could not remove category.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove category {CategoryId} from entity {EntityType}/{EntityId}", categoryId, entityType, entityId);
            return ApiResult.Fail("Could not remove category.");
        }
    }

    private async Task<IReadOnlyList<CategoryOptionVm>> SafeAvailableCategoriesAsync(CancellationToken ct)
    {
        try
        {
            var r = await _categories.GetCategoriesAsync(includeInactive: true, ct);
            if (!r.IsSuccess || r.Data is null)
                return [];

            var flat = new List<CategoryOptionVm>();
            void Walk(IEnumerable<CategoryItemResponse> nodes)
            {
                foreach (var n in nodes)
                {
                    flat.Add(n.ToOption());
                    if (n.Children.Count > 0)
                        Walk(n.Children);
                }
            }
            Walk(r.Data);
            return flat.OrderBy(o => o.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load categories master list");
            return [];
        }
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess)
            return ApiResult.Ok();
        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}

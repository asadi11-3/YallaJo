using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.EntityTags;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class EntityTagsFacade
{
    private readonly EntityTagsApiClient _entityTags;
    private readonly TagsApiClient _tags;
    private readonly ILogger<EntityTagsFacade> _logger;

    public EntityTagsFacade(
        EntityTagsApiClient entityTags,
        TagsApiClient tags,
        ILogger<EntityTagsFacade> logger)
    {
        _entityTags = entityTags;
        _tags = tags;
        _logger = logger;
    }

    /// <summary>
    /// Builds the page view model. When <paramref name="entityType"/> and
    /// <paramref name="entityId"/> are supplied, also loads the entity's current
    /// tag assignments. The master tag list is always loaded for the assign form.
    /// </summary>
    public async Task<ApiResult<EntityTagsVm>> GetAsync(
        string? entityType, Guid? entityId, CancellationToken ct = default)
    {
        var available = await SafeAvailableTagsAsync(ct);

        var vm = new EntityTagsVm
        {
            AvailableTags = available,
            Query = new EntityTagsQueryVm
            {
                EntityType = entityType ?? string.Empty,
                EntityId = entityId ?? Guid.Empty
            }
        };

        var hasQuery = !string.IsNullOrWhiteSpace(entityType) && entityId is { } id && id != Guid.Empty;
        if (!hasQuery)
            return ApiResult<EntityTagsVm>.CreateSuccess(vm);

        var assigned = await _entityTags.GetAssignedAsync(entityType!.Trim(), entityId!.Value, ct);
        if (assigned.RequireSignOut)
            return ApiResult<EntityTagsVm>.ForceSignOut();
        if (!assigned.IsSuccess || assigned.Data is null)
            return ApiResult<EntityTagsVm>.CreateFailure(assigned.StatusCode, assigned.Error);

        vm.HasQueried = true;
        vm.Assigned = assigned.Data.Select(t => t.ToRow()).ToList();
        return ApiResult<EntityTagsVm>.CreateSuccess(vm);
    }

    public async Task<ApiResult> AssignAsync(
        string entityType, Guid entityId, IReadOnlyList<Guid> tagIds, CancellationToken ct = default)
    {
        if (tagIds.Count == 0)
            return ApiResult.Invalid(new Dictionary<string, string[]>
            {
                ["TagIds"] = ["Select at least one tag to assign."]
            });

        var request = new AssignTagsToEntityRequest(entityType.Trim(), entityId, tagIds);
        try
        {
            var result = await _entityTags.AssignAsync(request, ct);
            return Normalize(result, "Unable to assign the selected tags.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to assign tags to {EntityType}/{EntityId}", entityType, entityId);
            return ApiResult.Fail("Unable to assign the selected tags. Please try again.");
        }
    }

    public async Task<ApiResult> RemoveAsync(
        string entityType, Guid entityId, Guid tagId, CancellationToken ct = default)
    {
        try
        {
            var result = await _entityTags.RemoveAsync(entityType.Trim(), entityId, tagId, ct);
            return Normalize(result, "Unable to remove the tag.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove tag {TagId} from {EntityType}/{EntityId}", tagId, entityType, entityId);
            return ApiResult.Fail("Unable to remove the tag. Please try again.");
        }
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsSuccess) return ApiResult.Ok();
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private async Task<IReadOnlyList<TagOptionVm>> SafeAvailableTagsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _tags.GetTagsAsync(activeOnly: false, ct);
            if (result is { IsSuccess: true, Data: not null })
                return result.Data.Select(t => t.ToOption()).OrderBy(t => t.Name).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load master tag list for entity-tag assignment.");
        }
        return [];
    }
}

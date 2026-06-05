using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.EntityTags;

/// <summary>
/// View model for the Entity Tags assignment manager page.
/// </summary>
public sealed class EntityTagsVm
{
    /// <summary>The entity-type / entity-id query that was run (if any).</summary>
    public EntityTagsQueryVm Query { get; set; } = new();

    /// <summary>True once a valid entityType + entityId has been queried.</summary>
    public bool HasQueried { get; set; }

    /// <summary>Tags currently assigned to the queried entity.</summary>
    public IReadOnlyList<EntityTagRowVm> Assigned { get; set; } = [];

    /// <summary>All tags available in the system, for the assign multi-select.</summary>
    public IReadOnlyList<TagOptionVm> AvailableTags { get; set; } = [];

    public bool HasAssigned => Assigned.Count > 0;
}

public sealed class EntityTagsQueryVm
{
    [Required(ErrorMessage = "Entity type is required.")]
    [StringLength(100)]
    [Display(Name = "Entity type")]
    public string EntityType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Entity id is required.")]
    [Display(Name = "Entity id")]
    public Guid EntityId { get; set; }
}

public sealed record EntityTagRowVm(Guid TagId, string Name, string Slug);

public sealed record TagOptionVm(Guid Id, string Name, string Slug, bool IsActive);

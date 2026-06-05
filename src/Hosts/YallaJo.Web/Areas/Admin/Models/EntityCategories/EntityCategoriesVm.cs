using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.EntityCategories;

public sealed class EntityCategoriesVm
{
    public EntityCategoriesQueryVm Query { get; set; } = new();

    public bool HasQueried { get; set; }

    public IReadOnlyList<EntityCategoryRowVm> Assigned { get; set; } = [];

    public IReadOnlyList<CategoryOptionVm> AvailableCategories { get; set; } = [];

    public bool HasAssigned => Assigned.Count > 0;
}

public sealed class EntityCategoriesQueryVm
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Entity type")]
    public string EntityType { get; set; } = "";

    [Required]
    [Display(Name = "Entity id")]
    public Guid EntityId { get; set; }
}

public sealed record EntityCategoryRowVm(Guid CategoryId, string Name, string Slug);

public sealed record CategoryOptionVm(Guid Id, string Name, string Slug, bool IsActive);

// <copyright file="SeoFaqVm.cs" company="YallaJo">
// Copyright (c) YallaJo. All rights reserved.
// </copyright>

namespace YallaJo.Web.Areas.Admin.Models.SeoFaq;

using System.ComponentModel.DataAnnotations;

public sealed class SeoFaqVm
{
    public IReadOnlyList<FaqItemRowVm> Items { get; set; } = [];

    public SeoEntityType? EntityTypeFilter { get; set; }

    public Guid? EntityIdFilter { get; set; }

    public bool? ActiveOnlyFilter { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public CreateFaqItemFormVm Form { get; set; } = new();

    public bool HasItems => this.Items.Count > 0;

    public int TotalPages => this.PageSize <= 0 ? 0 : (int)Math.Ceiling((double)this.TotalCount / this.PageSize);

    public bool HasPrevious => this.Page > 1;

    public bool HasNext => this.Page < this.TotalPages;
}

public sealed class FaqItemRowVm
{
    public Guid Id { get; set; }

    public SeoEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }
}

public sealed class CreateFaqItemFormVm
{
    [Required]
    [Display(Name = "Entity type")]
    public SeoEntityType EntityType { get; set; } = SeoEntityType.Tour;

    [Required]
    [Display(Name = "Entity id")]
    public Guid EntityId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    [Display(Name = "Question")]
    public string Question { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [Display(Name = "Answer")]
    public string Answer { get; set; } = string.Empty;

    [Range(0, 1000)]
    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}

public sealed class UpdateFaqItemFormVm
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    [Display(Name = "Question")]
    public string Question { get; set; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [Display(Name = "Answer")]
    public string Answer { get; set; } = string.Empty;
}

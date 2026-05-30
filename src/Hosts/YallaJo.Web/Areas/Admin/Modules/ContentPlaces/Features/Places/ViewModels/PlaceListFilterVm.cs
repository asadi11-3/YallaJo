using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Modules.ContentPlaces.Features.Places.ViewModels;

public sealed class PlaceListFilterVm
{
    [Display(Name = "Category Id")]
    public Guid? CategoryId { get; set; }

    [Range(0.0, 5.0)]
    [Display(Name = "Min rating")]
    public decimal? RatingMin { get; set; }

    [Range(0.0, 5.0)]
    [Display(Name = "Max rating")]
    public decimal? RatingMax { get; set; }

    [StringLength(200)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(200)]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [Display(Name = "Has active tours")]
    public bool? HasActiveTours { get; set; }
}

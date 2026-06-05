using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Business.Models.Amenities;

public sealed class AmenitiesVm
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string Status { get; set; } = "";
    public IReadOnlyList<AmenityRowVm> Amenities { get; set; } = [];
    public AddAmenityFormVm Form { get; set; } = new();
    public bool HasAmenities => Amenities.Count > 0;
}

public sealed record AmenityRowVm(Guid Id, string Name, string? Icon, int SortOrder);

public sealed class AddAmenityFormVm
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Amenity name")]
    public string Name { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Icon (optional)")]
    public string? Icon { get; set; }

    [Range(0, 1000)]
    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}

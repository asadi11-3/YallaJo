using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.Places;

public abstract class PlaceFormVm
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(300)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    [RegularExpression(@"^[a-z0-9\-]+$",
        ErrorMessage = "Slug must contain only lowercase letters, digits, and hyphens.")]
    [Display(Name = "Slug")]
    public string? Slug { get; set; }

    [Required]
    [Display(Name = "Place type")]
    public PlaceTypeOption PlaceType { get; set; } = PlaceTypeOption.Attraction;

    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    [Display(Name = "Latitude")]
    public decimal Latitude { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    [Display(Name = "Longitude")]
    public decimal Longitude { get; set; }

    [StringLength(4000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "Address")]
    public string? Address { get; set; }

    [StringLength(200)]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(200)]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [StringLength(50)]
    [Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [StringLength(50)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(320)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(500)]
    [Url]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    [StringLength(300)]
    [Display(Name = "Meta title")]
    public string? MetaTitle { get; set; }

    [StringLength(1000)]
    [Display(Name = "Meta description")]
    public string? MetaDescription { get; set; }
}

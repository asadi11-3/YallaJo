using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Provider.Models;

/// <summary>Form view model for registering a new provider application (Draft).</summary>
public sealed class ProviderApplyVm
{
    [Required(ErrorMessage = "Please choose a provider type.")]
    [Display(Name = "Provider type")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Business name is required.")]
    [StringLength(200)]
    [Display(Name = "Business name")]
    public string BusinessName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Contact email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact phone is required.")]
    [StringLength(40)]
    [Display(Name = "Contact phone")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "A short description is required.")]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Available provider-type options (enum names) for the dropdown.</summary>
    public IReadOnlyList<ProviderTypeOptionVm> TypeOptions { get; init; } = [];
}

/// <summary>A selectable provider type for the apply dropdown.</summary>
public sealed class ProviderTypeOptionVm
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

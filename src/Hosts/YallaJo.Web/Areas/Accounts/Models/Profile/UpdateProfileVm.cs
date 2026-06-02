using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Accounts.Models.Profile;

public sealed class UpdateProfileVm
{
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name must be 100 characters or fewer.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name must be 100 characters or fewer.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Display(Name = "Gender")]
    public GenderOption? Gender { get; set; }

    [StringLength(100, ErrorMessage = "Country must be 100 characters or fewer.")]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [StringLength(100, ErrorMessage = "City must be 100 characters or fewer.")]
    [Display(Name = "City")]
    public string? City { get; set; }

    [StringLength(300, ErrorMessage = "Address line must be 300 characters or fewer.")]
    [Display(Name = "Address")]
    public string? AddressLine { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Accounts.Features.UpdatePhone.ViewModels;

public sealed class UpdatePhoneVm
{
    [Required(ErrorMessage = "Phone number is required.")]
    [Display(Name = "Phone number")]
    [RegularExpression(@"^\+?[0-9\s\-()]{7,20}$", ErrorMessage = "Invalid phone number format.")]
    [StringLength(20, ErrorMessage = "Phone number must be 20 characters or fewer.")]
    public string PhoneNumber { get; set; } = string.Empty;
}

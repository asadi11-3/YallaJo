using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YallaJo.Web.Areas.Accounts.Models.Profile;

public sealed class UpdateAvatarVm
{
    [Required(ErrorMessage = "Please choose an image file.")]
    [Display(Name = "Avatar")]
    public IFormFile? File { get; set; }
}

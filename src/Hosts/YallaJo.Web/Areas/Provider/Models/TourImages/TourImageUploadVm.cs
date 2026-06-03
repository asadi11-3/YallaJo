using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YallaJo.Web.Areas.Provider.Models.TourImages;

public sealed class TourImageUploadVm
{
    public const string AcceptAttribute = "image/png,image/jpeg,.png,.jpg,.jpeg";

    [Required(ErrorMessage = "Please choose an image to upload.")]
    [Display(Name = "Image")]
    public IFormFile? File { get; set; }
}

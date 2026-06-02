using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YallaJo.Web.Areas.Admin.Models.Attachments;

public sealed class UploadAttachmentVm
{
    [Required(ErrorMessage = "File is required.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    [Required]
    [Display(Name = "Entity type")]
    public EntityTypeOption EntityType { get; set; }

    [Required]
    [Display(Name = "Entity ID")]
    public Guid EntityId { get; set; }

    [Required]
    [Display(Name = "Attachment type")]
    public AttachmentTypeOption AttachmentType { get; set; }

    [Display(Name = "Width (px)")]
    public int? Width { get; set; }

    [Display(Name = "Height (px)")]
    public int? Height { get; set; }

    [Display(Name = "Duration (sec)")]
    public int? DurationSeconds { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Sort order")]
    public int SortOrder { get; set; }
}

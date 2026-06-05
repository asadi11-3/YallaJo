using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YallaJo.Web.Areas.Provider.Models;

public sealed class ProviderDocumentUploadVm
{
    public static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];

    public const long MaxFileSizeBytes = 10L * 1024 * 1024;

    public const string AcceptAttribute = ".pdf,.jpg,.jpeg,.png,application/pdf,image/jpeg,image/png";

    [Required(ErrorMessage = "Please choose a document type.")]
    [Display(Name = "Document type")]
    public string DocumentType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a file to upload.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }

    [Display(Name = "Expires at")]
    [DataType(DataType.Date)]
    public DateTime? ExpiresAt { get; set; }

    public IReadOnlyList<ProviderDocumentTypeOptionVm> DocumentTypeOptions { get; init; } = [];
}

public sealed class ProviderDocumentTypeOptionVm
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// Posted by the inline "replace document" form on the status page. Targets a single
/// existing document by <see cref="DocumentId"/> and swaps in new file metadata via
/// <c>PUT /api/v1/provider/documents/{id}</c>.
/// </summary>
public sealed class ReplaceProviderDocumentVm
{
    [Required]
    public Guid DocumentId { get; set; }

    [Required(ErrorMessage = "A file URL is required.")]
    [Url(ErrorMessage = "Enter a valid file URL.")]
    [StringLength(2048)]
    [Display(Name = "File URL")]
    public string FileUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "A file name is required.")]
    [StringLength(256)]
    [Display(Name = "File name")]
    public string FileName { get; set; } = string.Empty;

    [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than zero.")]
    [Display(Name = "File size (bytes)")]
    public long FileSizeBytes { get; set; }

    [Display(Name = "Expires at")]
    [DataType(DataType.Date)]
    public DateTime? ExpiresAt { get; set; }
}

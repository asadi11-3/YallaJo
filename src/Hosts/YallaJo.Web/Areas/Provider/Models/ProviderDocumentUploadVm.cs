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
/// existing document by <see cref="DocumentId"/> and uploads a real replacement file via
/// <c>POST /api/v1/provider/documents/{id}/replace-upload</c> (multipart).
/// Replaced the old manual FileUrl/FileName/FileSizeBytes inputs per F10/UX plan Phase 5.
/// </summary>
public sealed class ReplaceProviderDocumentVm
{
    [Required]
    public Guid DocumentId { get; set; }

    [Required(ErrorMessage = "Please choose a file to upload.")]
    [Display(Name = "New file")]
    public IFormFile? File { get; set; }

    [Display(Name = "Expires at")]
    [DataType(DataType.Date)]
    public DateTime? ExpiresAt { get; set; }
}

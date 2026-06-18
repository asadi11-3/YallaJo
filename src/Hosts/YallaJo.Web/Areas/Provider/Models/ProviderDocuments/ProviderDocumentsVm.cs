using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace YallaJo.Web.Areas.Provider.Models.ProviderDocuments;

public sealed class ProviderDocumentsIndexVm
{
    public IReadOnlyList<ProviderDocumentRowVm> Documents { get; init; } = [];
    public UploadProviderDocumentFormVm Upload { get; set; } = new();

    public bool HasDocuments => Documents.Count > 0;
}

public sealed class ProviderDocumentRowVm
{
    public Guid Id { get; init; }
    public DocumentType Type { get; init; }
    public string TypeLabel { get; init; } = "";
    public string? FileName { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DocumentStatus Status { get; init; }
    public string? RejectionReason { get; init; }
    public string RowVersion { get; init; } = "";

    public bool IsExpiringSoon => ExpiresAt is { } e && e > DateTime.UtcNow && e <= DateTime.UtcNow.AddDays(30);

    // A11Y5: colour + icon + text.
    public (string Css, string Icon) StatusBadge => Status switch
    {
        DocumentStatus.Approved => ("text-bg-success", "bi-check-circle"),
        DocumentStatus.Rejected => ("text-bg-danger", "bi-x-circle"),
        DocumentStatus.Expired  => ("text-bg-warning", "bi-clock-history"),
        _                       => ("text-bg-secondary", "bi-hourglass-split"),
    };
}

public sealed class UploadProviderDocumentFormVm
{
    [Required(ErrorMessage = "Choose a document type.")]
    [Display(Name = "Document type")]
    public DocumentType Type { get; set; } = DocumentType.Other;

    [DataType(DataType.Date)]
    [Display(Name = "Expiry date (optional)")]
    public DateOnly? ExpiresAt { get; set; }

    [Required(ErrorMessage = "Choose a file to upload.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }
}

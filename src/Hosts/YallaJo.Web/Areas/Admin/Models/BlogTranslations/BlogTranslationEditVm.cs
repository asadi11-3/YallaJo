using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Admin.Models.BlogTranslations;

/// <summary>
/// Form view model for adding/editing a single blog translation.
/// LanguageCode comes from the route (display-only); no LanguageId in the form.
/// </summary>
public sealed class BlogTranslationEditVm : IValidatableObject
{
    public Guid BlogId { get; set; }

    /// <summary>2-letter language code (e.g. "ar"). Route-supplied, shown read-only.</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>True when no translation exists yet for this language (empty form).</summary>
    public bool IsNew { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Summary cannot exceed 1000 characters.")]
    public string? Summary { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Mirror the API rule: content must be at least 150 chars after stripping HTML.
        // This is a client-side hint; the API remains the source of truth.
        if (!string.IsNullOrWhiteSpace(Content) && CountVisibleChars(Content) < 150)
        {
            yield return new ValidationResult(
                "Content must be at least 150 characters (excluding HTML).",
                [nameof(Content)]);
        }
    }

    // Strips angle-bracket tags and counts the remaining characters.
    private static int CountVisibleChars(string html)
    {
        var inTag = false;
        var count = 0;
        foreach (var ch in html)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; continue; }
            if (!inTag && !char.IsWhiteSpace(ch)) count++;
        }
        return count;
    }
}

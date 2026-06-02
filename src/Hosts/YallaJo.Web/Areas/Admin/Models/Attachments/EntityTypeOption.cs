namespace YallaJo.Web.Areas.Admin.Models.Attachments;

/// <summary>
/// Mirrors ContentCore.Domain.Enums.EntityType — string values used for the
/// backend's Enum.TryParse lookup (endpoint parses string input).
/// </summary>
public enum EntityTypeOption
{
    Place,
    Tour,
    Business,
    Review,
    Blog,
    TourGuide,
}

/// <summary>
/// Mirrors ContentCore.Domain.Enums.AttachmentType.
/// </summary>
public enum AttachmentTypeOption
{
    Image,
    Video,
    Document,
    Audio,
}

namespace YallaJo.Web.Areas.Guide.Models.Profile;

/// <summary>
/// Request body for PUT /api/v1/guides/{guideId} (update core guide profile fields).
/// </summary>
public sealed record UpdateTourGuideProfileRequest(
    string Bio,
    int YearsOfExperience,
    bool HasFirstAid,
    string? MoTALicenseNumber);

/// <summary>
/// Request body for POST /api/v1/guides/{guideId}/languages.
/// Proficiency must be one of: Native, Fluent, Conversational, Basic.
/// </summary>
public sealed record AddTourGuideLanguageRequest(Guid LanguageId, string Proficiency);

/// <summary>
/// Request body for POST /api/v1/guides/{guideId}/specializations.
/// </summary>
public sealed record AddTourGuideSpecializationRequest(Guid SpecializationId);

/// <summary>
/// Request body for PUT /api/v1/guides/me/avatar (persists a previously uploaded URL).
/// </summary>
public sealed record UpdateGuideAvatarRequest(string AvatarUrl);

/// <summary>
/// GET /api/v1/content-core/specializations?activeOnly=true item.
/// </summary>
public sealed record SpecializationResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Icon,
    bool IsActive);

/// <summary>
/// GET /api/v1/content-core/languages?activeOnly=true item.
/// </summary>
public sealed record LanguageResponse(
    Guid Id,
    string Name,
    string? Code,
    bool IsActive);

/// <summary>
/// Response from POST /api/v1/content-core/attachments (multipart upload), carrying the persisted URL.
/// </summary>
public sealed record UploadAttachmentResponse(Guid Id, string? Url);

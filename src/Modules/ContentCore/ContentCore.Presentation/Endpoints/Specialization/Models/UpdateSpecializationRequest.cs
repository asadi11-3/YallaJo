namespace ContentCore.Presentation.Endpoints.Specialization.Models;

public sealed record UpdateSpecializationRequest(
    string Name,
    string? Description = null,
    string? Icon = null,
    bool? IsActive = null,
    string SourceLanguageCode = "en");

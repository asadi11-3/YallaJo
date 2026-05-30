namespace ContentCore.Presentation.Endpoints.Specialization.Models;

public sealed record CreateSpecializationRequest(
    string Name,
    string? Description = null,
    string? Icon = null,
    string SourceLanguageCode = "en");

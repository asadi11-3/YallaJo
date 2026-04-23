namespace ContentCore.Presentation.Endpoints.Tag.Models;

public sealed record UpdateTagRequest(string Name, string Slug, string SourceLanguageCode = "en");

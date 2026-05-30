namespace YallaJo.Api.Services;

public interface ISeoRedirectLookupService
{
    Task<SeoRedirectEntry?> FindRedirectAsync(string sourcePath, CancellationToken ct);

    Task RecordHitAsync(Guid redirectId, CancellationToken ct);
}

public sealed record SeoRedirectEntry(Guid Id, string SourcePath, string TargetPath, bool IsPermanent);

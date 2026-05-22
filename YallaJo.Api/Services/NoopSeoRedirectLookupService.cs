namespace YallaJo.Api.Services;

internal sealed class NoopSeoRedirectLookupService : ISeoRedirectLookupService
{
    public Task<SeoRedirectEntry?> FindRedirectAsync(string sourcePath, CancellationToken ct)
        => Task.FromResult<SeoRedirectEntry?>(null);

    public Task RecordHitAsync(Guid redirectId, CancellationToken ct)
        => Task.CompletedTask;
}

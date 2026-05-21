using YallaJo.SharedKernel.Infrastructure.Data;

namespace Analytics.Infrastructure.Persistence.Seeding;

internal sealed class AnalyticsDbInitializer : IModuleDbInitializer
{
    public int Order => 0;

    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}

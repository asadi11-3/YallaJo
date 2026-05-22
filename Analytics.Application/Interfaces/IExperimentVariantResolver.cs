namespace Analytics.Application.Interfaces;

public interface IExperimentVariantResolver
{
    Task<string?> GetVariantAsync(Guid userId, string experimentName, CancellationToken ct = default);
}

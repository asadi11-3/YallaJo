using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
namespace Analytics.Infrastructure.Services;

/// <summary>
/// Deterministic A/B experiment bucketing using stable hash.
/// Hash(UserId, ExperimentId) % 100 &lt; TrafficPercent → in experiment.
/// Consistent hashing picks variant from experiment's variant list.
/// </summary>
public sealed class ExperimentVariantResolver(
    IExperimentRepository experimentRepo) : IExperimentVariantResolver
{
    public async Task<string?> GetVariantAsync(Guid userId, string experimentName, CancellationToken ct = default)
    {
        var experiment = await experimentRepo.GetByNameAsync(experimentName, ct);
        if (experiment is null || !experiment.IsRunning(DateTime.UtcNow))
            return null;

        // Check existing assignment
        var existing = await experimentRepo.GetAssignmentAsync(userId, experiment.Id, ct);
        if (existing is not null)
            return existing.VariantName;

        // Stable hash bucketing
        var bucket = StableHash(userId, experiment.Id) % 100;
        if (bucket >= experiment.TrafficPercent)
            return null; // Not in experiment

        // Parse variants
        var variants = JsonSerializer.Deserialize<string[]>(experiment.VariantsJson) ?? [];
        if (variants.Length == 0) return null;

        // Consistent variant assignment
        var variantIndex = (int)(StableHash(userId, experiment.Id) % (uint)variants.Length);
        var variantName = variants[variantIndex];

        // Persist assignment
        var assignment = ExperimentAssignment.Create(userId, experiment.Id, variantName);
        await experimentRepo.AddAssignmentAsync(assignment, ct);

        return variantName;
    }

    private static uint StableHash(Guid userId, Guid experimentId)
    {
        var input = $"{userId}:{experimentId}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToUInt32(hash, 0);
    }
}

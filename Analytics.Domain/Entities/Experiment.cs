using YallaJo.SharedKernel.Domain.Entities;

namespace Analytics.Domain.Entities;

public sealed class Experiment : BaseEntity, IAggregateRoot
{
    private Experiment() { }

    public string Name { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Draft"; // Draft, Running, Paused, Completed
    public DateTime StartsAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public int TrafficPercent { get; private set; }
    public string VariantsJson { get; private set; } = "[]"; // JSON array of variant names

    public static Experiment Create(string name, DateTime startsAt, DateTime expiresAt, int trafficPercent, string variantsJson)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (expiresAt <= startsAt)
            throw new ArgumentException("Expiration must be after start.", nameof(expiresAt));
        if (trafficPercent is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(trafficPercent), "Traffic percent must be 1-100.");

        return new Experiment
        {
            Name = name,
            Status = "Draft",
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            TrafficPercent = trafficPercent,
            VariantsJson = variantsJson
        };
    }

    public void Start() => Status = "Running";
    public void Pause() => Status = "Paused";
    public void Complete() => Status = "Completed";

    public bool IsRunning(DateTime now) => Status == "Running" && now >= StartsAt && now < ExpiresAt;
}

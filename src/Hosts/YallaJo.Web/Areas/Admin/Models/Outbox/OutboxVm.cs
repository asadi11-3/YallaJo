namespace YallaJo.Web.Areas.Admin.Models.Outbox;

public sealed class OutboxVm
{
    public IReadOnlyList<DeadLetterRowVm> DeadLetters { get; set; } = [];
    public int TotalCount { get; set; }
    public string? ModuleFilter { get; set; }
}

public sealed class DeadLetterRowVm
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

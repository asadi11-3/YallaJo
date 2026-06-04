namespace YallaJo.Web.Areas.Admin.Models.Outbox;

public static class OutboxMapper
{
    public static OutboxVm ToVm(DeadLetterPageResponse page, string? moduleFilter)
    {
        return new OutboxVm
        {
            TotalCount = page.TotalCount,
            ModuleFilter = moduleFilter,
            DeadLetters = page.Items.Select(d => new DeadLetterRowVm
            {
                Id = d.Id,
                Module = d.Module,
                Type = d.Type,
                OccurredOnUtc = d.OccurredOnUtc,
                RetryCount = d.RetryCount,
                LastError = d.LastError,
            }).ToList(),
        };
    }
}

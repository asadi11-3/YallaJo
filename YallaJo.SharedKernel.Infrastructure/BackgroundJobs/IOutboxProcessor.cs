namespace YallaJo.SharedKernel.Infrastructure.BackgroundJobs
{
    public interface IOutboxProcessor
    {
        Task ProcessOutboxMessagesAsync(CancellationToken ct = default);
    }
}

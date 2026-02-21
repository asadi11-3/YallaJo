namespace YallaJo.SharedKernel.Application.Abstractions.Clock
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
        DateTime Today => UtcNow.Date;
    }
}

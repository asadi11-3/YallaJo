using YallaJo.SharedKernel.Application.Abstractions.Clock;

namespace YallaJo.SharedKernel.Infrastructure.Clock
{
    internal sealed class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

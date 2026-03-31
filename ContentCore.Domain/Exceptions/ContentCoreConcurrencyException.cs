namespace ContentCore.Domain.Exceptions;

/// <summary>
/// Thrown by <c>ContentCoreUnitOfWork.SaveChangesAsync</c> when EF Core detects a
/// concurrency conflict (optimistic locking row-version mismatch).
/// Allows Application layer handlers to respond to concurrency failures
/// without depending on Microsoft.EntityFrameworkCore.
/// </summary>
public sealed class ContentCoreConcurrencyException : Exception
{
    private const string DefaultMessage =
        "A concurrency conflict occurred. Please refresh and try again.";

    public ContentCoreConcurrencyException()
        : base(DefaultMessage)
    {
    }

    public ContentCoreConcurrencyException(string message)
        : base(message)
    {
    }

    public ContentCoreConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

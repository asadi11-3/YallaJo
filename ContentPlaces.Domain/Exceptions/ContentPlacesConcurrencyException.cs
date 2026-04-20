namespace ContentPlaces.Domain.Exceptions;

/// <summary>
/// Thrown by <c>ContentPlacesUnitOfWork.SaveChangesAsync</c> when EF Core detects a
/// concurrency conflict (optimistic locking row-version mismatch).
/// Allows Application layer handlers to respond to concurrency failures
/// without depending on Microsoft.EntityFrameworkCore.
/// </summary>
public sealed class ContentPlacesConcurrencyException : Exception
{
    private const string DefaultMessage =
        "A concurrency conflict occurred. Please refresh and try again.";

    public ContentPlacesConcurrencyException()
        : base(DefaultMessage)
    {
    }

    public ContentPlacesConcurrencyException(string message)
        : base(message)
    {
    }

    public ContentPlacesConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

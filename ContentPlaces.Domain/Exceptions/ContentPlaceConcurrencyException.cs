namespace ContentPlaces.Domain.Exceptions;

public sealed class ContentPlaceConcurrencyException : Exception
{
    private const string DefaultMessage =
        "A concurrency conflict occurred. Please refresh and try again.";

    public ContentPlaceConcurrencyException()
        : base(DefaultMessage)
    {
    }

    public ContentPlaceConcurrencyException(string message)
        : base(message)
    {
    }

    public ContentPlaceConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

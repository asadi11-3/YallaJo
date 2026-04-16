namespace ContentCore.Domain.Exceptions;


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

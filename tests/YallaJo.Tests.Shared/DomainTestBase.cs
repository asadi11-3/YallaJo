namespace YallaJo.Tests.Shared;

public abstract class DomainTestBase
{
    protected const string DefaultSourceLanguageCode = "en";

    protected static string RandomSlug(string prefix = "category")
        => $"{prefix}-{Guid.NewGuid():N}";
}

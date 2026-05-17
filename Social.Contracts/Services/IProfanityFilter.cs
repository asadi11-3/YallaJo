namespace Social.Contracts.Services;

public interface IProfanityFilter
{
    bool ContainsProfanity(string text);
    string Sanitize(string text);
}

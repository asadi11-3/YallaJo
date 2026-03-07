using ContentCore.Domain.Events;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Language : AuditableEntity, IAggregateRoot
{
    private Language() { } // EF Core

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string NativeName { get; private set; } = string.Empty;
    public bool IsRtl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Language Create(
        string code,
        string name,
        string nativeName,
        bool isRtl)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Language code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Language name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(nativeName))
            throw new ArgumentException("Native name is required.", nameof(nativeName));

        var language = new Language
        {
            Code = code.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            NativeName = nativeName.Trim(),
            IsRtl = isRtl,
            IsActive = true
        };

        language.AddDomainEvent(new LanguageActivatedDomainEvent(language.Id, language.Code));

        return language;
    }

    public void Update(string name, string nativeName, bool isRtl)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Language name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(nativeName))
            throw new ArgumentException("Native name is required.", nameof(nativeName));

        Name = name.Trim();
        NativeName = nativeName.Trim();
        IsRtl = isRtl;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        AddDomainEvent(new LanguageActivatedDomainEvent(Id, Code));
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}

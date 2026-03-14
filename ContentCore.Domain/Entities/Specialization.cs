using System;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Specialization : AuditableEntity
{
    private Specialization() { } // EF Core

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Icon { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Specialization Create(string name, string? description = null, string? icon = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Specialization name cannot be empty.", nameof(name));

        return new Specialization
        {
            Name = name.Trim(),
            Description = description,
            Icon = icon,
            IsActive = true
        };
    }

    public void Update(string name, string? description, string? icon)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Specialization name cannot be empty.", nameof(name));

        Name = name.Trim();
        Description = description;
        Icon = icon;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}

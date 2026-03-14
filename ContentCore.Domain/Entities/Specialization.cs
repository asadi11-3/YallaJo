using System;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Specialization : AuditableEntity
{
	public string Name { get; private set; } = string.Empty;
	public string? Description { get; private set; }
	public string? Icon { get; private set; }
	public bool IsActive { get; private set; } = true;

	private Specialization() { }

	public static Specialization Create(string name, string? description, string? icon, bool isActive = true)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Specialization name is required.", nameof(name));

		return new Specialization
		{
			Name = name.Trim(),
			Description = description?.Trim(),
			Icon = icon?.Trim(),
			IsActive = isActive
		};
	}

	public void Update(string name, string? description, string? icon, bool isActive)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Specialization name is required.", nameof(name));

		Name = name.Trim();
		Description = description?.Trim();
		Icon = icon?.Trim();
		IsActive = isActive;

		MarkUpdated();
	}
}
using System;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentCore.Domain.Entities;

public sealed class Tag : AuditableEntity<Guid>
{
	public string Name { get; private set; } = string.Empty;
	public string Slug { get; private set; } = string.Empty;
	public bool IsActive { get; private set; } = true;

	private Tag() { } // EF Core

	public static Tag Create(string name, string slug, bool isActive = true)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Tag name is required.", nameof(name));

		if (string.IsNullOrWhiteSpace(slug))
			throw new ArgumentException("Tag slug is required.", nameof(slug));

		return new Tag
		{
			Name = name.Trim(),
			Slug = slug.Trim().ToLowerInvariant(),
			IsActive = isActive
		};
	}

	public void Update(string name, string slug, bool isActive)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Tag name is required.", nameof(name));

		if (string.IsNullOrWhiteSpace(slug))
			throw new ArgumentException("Tag slug is required.", nameof(slug));

		Name = name.Trim();
		Slug = slug.Trim().ToLowerInvariant(); 
		IsActive = isActive;

		MarkUpdated();
	}
}
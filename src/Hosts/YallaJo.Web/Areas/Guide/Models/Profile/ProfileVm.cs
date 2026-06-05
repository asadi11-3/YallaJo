using System.ComponentModel.DataAnnotations;

namespace YallaJo.Web.Areas.Guide.Models.Profile;

/// <summary>
/// View model for the guide "My Profile" page.
/// </summary>
public sealed class ProfileVm
{
    public Guid GuideId { get; set; }

    // Display / header
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int TourCount { get; set; }

    // Editable core fields (bound on POST guide/profile/update)
    public ProfileFormVm Form { get; set; } = new();

    // Languages currently attached to the guide.
    public IReadOnlyList<GuideLanguageVm> Languages { get; set; } = [];

    // Specializations currently attached to the guide.
    public IReadOnlyList<GuideSpecializationVm> Specializations { get; set; } = [];

    // Lookup options for the "add" forms.
    public IReadOnlyList<OptionVm> AvailableLanguages { get; set; } = [];
    public IReadOnlyList<OptionVm> AvailableSpecializations { get; set; } = [];

    public static IReadOnlyList<string> ProficiencyOptions { get; } =
        ["Native", "Fluent", "Conversational", "Basic"];
}

/// <summary>
/// Bindable form for the core guide profile fields.
/// </summary>
public sealed class ProfileFormVm
{
    [Required]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Bio is required.")]
    public string Bio { get; set; } = "";

    [Range(0, 80, ErrorMessage = "Years of experience must be between 0 and 80.")]
    [Display(Name = "Years of experience")]
    public int YearsOfExperience { get; set; }

    [Display(Name = "Holds a valid first-aid certification")]
    public bool HasFirstAid { get; set; }

    [StringLength(64)]
    [Display(Name = "MoTA license number")]
    public string? MoTALicenseNumber { get; set; }
}

public sealed class GuideLanguageVm
{
    public Guid LanguageId { get; set; }
    public string Name { get; set; } = "";
    public string Proficiency { get; set; } = "";
}

public sealed class GuideSpecializationVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
}

public sealed record OptionVm(Guid Id, string Name);

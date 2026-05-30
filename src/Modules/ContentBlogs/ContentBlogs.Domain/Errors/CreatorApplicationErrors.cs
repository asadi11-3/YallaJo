using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentBlogs.Domain.Errors;

/// <summary>Domain errors for <see cref="Entities.Creators.CreatorApplication"/>.</summary>
public static class CreatorApplicationErrors
{
    public static readonly Error NotFound =
        new("CreatorApplication.NotFound", "Creator application was not found.");

    public static readonly Error AlreadySubmitted =
        new("CreatorApplication.AlreadySubmitted", "This application has already been submitted.");

    public static readonly Error NotPending =
        new("CreatorApplication.NotPending", "Only pending applications can be approved, rejected, or have more info requested.");

    public static readonly Error MaxReapplicationsReached =
        new("CreatorApplication.MaxReapplicationsReached", "Maximum number of re-applications has been reached.");

    public static readonly Error CoolingPeriodActive =
        new("CreatorApplication.CoolingPeriodActive", "A cooling period is still active. Please wait before re-applying.");

    public static readonly Error NotDraft =
        new("CreatorApplication.NotDraft", "Only draft applications can be submitted.");

    public static readonly Error NotMoreInfoNeeded =
        new("CreatorApplication.NotMoreInfoNeeded", "Only applications in 'MoreInfoNeeded' status can be resubmitted.");

    public static readonly Error AlreadyHasActiveApplication =
        new("CreatorApplication.AlreadyHasActiveApplication", "User already has an active (Draft or Pending) application.");
}

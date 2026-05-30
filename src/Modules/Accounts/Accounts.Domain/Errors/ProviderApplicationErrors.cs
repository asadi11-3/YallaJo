using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Accounts.Domain.Errors;

public static class ProviderApplicationErrors
{
    public static readonly Error NotFound =
        new("ProviderApplication.NotFound", "Provider application not found.");

    public static readonly Error AlreadyExists =
        new("ProviderApplication.AlreadyExists", "A provider application already exists for this user.");

    public static readonly Error AlreadyApproved =
        new("ProviderApplication.AlreadyApproved", "This provider application has already been approved.");

    public static readonly Error InvalidStatus =
        new("ProviderApplication.InvalidStatus", "Action is not allowed in the current application status.");

    public static readonly Error CoolingPeriodActive =
        new("ProviderApplication.CoolingPeriodActive", "You must wait for the cooling period to end before re-applying.");

    public static readonly Error MaxReapplicationsReached =
        new("ProviderApplication.MaxReapplicationsReached", "Maximum number of re-applications (3) has been reached.");

    public static readonly Error TooManyDocuments =
        new("ProviderApplication.TooManyDocuments", "Maximum of 10 documents per application.");

    public static readonly Error DocumentNotFound =
        new("ProviderApplication.DocumentNotFound", "The specified document was not found on this application.");

    public static readonly Error NotOwner =
        new("ProviderApplication.NotOwner", "You do not own this provider application.");

    public static readonly Error MissingRequiredDocuments =
        new("ProviderApplication.MissingRequiredDocuments", "Not all required documents have been uploaded for your provider type.");

    public static readonly Error DuplicateDocumentType =
        new("ProviderApplication.DuplicateDocumentType", "A document of this type has already been uploaded. Use the replace endpoint to update it.");
}

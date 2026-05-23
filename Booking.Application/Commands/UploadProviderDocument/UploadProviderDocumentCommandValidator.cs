using System;
using FluentValidation;
using Booking.Domain.Enums;

namespace Booking.Application.Commands.UploadProviderDocument;

public sealed class UploadProviderDocumentCommandValidator : AbstractValidator<UploadProviderDocumentCommand>
{
    public UploadProviderDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();

        RuleFor(x => x.File).NotNull().WithMessage("File is required.");

        RuleFor(x => x.ExpiresAt)
            .NotNull()
            .GreaterThan(DateTime.UtcNow.Date)
            .When(x => DocumentTypeRequiresExpiry(x.DocumentType))
            .WithMessage("Expiry date required for this document type and must be in the future.");
    }

    private static bool DocumentTypeRequiresExpiry(DocumentType type)
    {
        return type is DocumentType.MoTALicense
                    or DocumentType.BusinessLicense
                    or DocumentType.TaxRegistration
                    or DocumentType.InsuranceCertificate
                    or DocumentType.LiabilityInsurance
                    or DocumentType.HealthSafetyCertificate
                    or DocumentType.FireSafetyCertificate
                    or DocumentType.ActivityCertification
                    or DocumentType.TourismAuthorityLicense
                    or DocumentType.FirstAidCertification;
    }
}

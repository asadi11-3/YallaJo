using Accounts.Application.Commands.Admin.RejectProvider;
using Accounts.Application.Commands.Admin.RequestMoreDocs;
using Accounts.Application.Commands.Admin.SuspendProvider;
using Accounts.Application.Commands.Provider.AddDocument;
using Accounts.Application.Commands.Provider.RegisterProvider;
using Accounts.Application.Commands.Provider.ReplaceDocument;
using Accounts.Domain.Enums;
using FluentAssertions;

namespace Accounts.Tests.Unit;

/// <summary>
/// FluentValidation tests for provider-related command validators.
/// Verifies required-field enforcement, length limits, and enum validation.
/// </summary>
public sealed class ProviderCommandValidatorTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // RegisterProviderCommandValidator
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly RegisterProviderCommandValidator RegisterValidator = new();

    private static RegisterProviderCommand ValidRegisterCommand() => new(
        Type:                ProviderType.TourOperator,
        BusinessName:        "Acme Tours",
        ContactEmail:        "info@acme.com",
        ContactPhone:        "+1234567890",
        Address:             "123 Main St, Cairo",
        Description:         "Premium tour operator",
        TypeSpecificDataJson: null);

    [Fact]
    public void RegisterValidator_ValidCommand_IsValid()
    {
        var result = RegisterValidator.Validate(ValidRegisterCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterValidator_EmptyBusinessName_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { BusinessName = "" };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BusinessName");
    }

    [Fact]
    public void RegisterValidator_BusinessNameExceeds200Chars_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { BusinessName = new string('A', 201) };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterValidator_InvalidEmail_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { ContactEmail = "not-an-email" };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactEmail");
    }

    [Fact]
    public void RegisterValidator_EmptyPhone_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { ContactPhone = "" };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterValidator_EmptyAddress_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { Address = "" };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterValidator_EmptyDescription_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { Description = "" };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterValidator_InvalidProviderType_IsInvalid()
    {
        var cmd = ValidRegisterCommand() with { Type = (ProviderType)99 };
        var result = RegisterValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AddProviderDocumentCommandValidator
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly AddProviderDocumentCommandValidator AddDocValidator = new();

    private static AddProviderDocumentCommand ValidAddDocCommand() => new(
        DocumentType:  DocumentType.BusinessLicense,
        FileUrl:       "https://storage.example.com/docs/license.pdf",
        FileName:      "license.pdf",
        FileSizeBytes: 1024 * 512,
        ExpiresAt:     DateTime.UtcNow.AddYears(1));

    [Fact]
    public void AddDocValidator_ValidCommand_IsValid()
    {
        var result = AddDocValidator.Validate(ValidAddDocCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddDocValidator_EmptyFileUrl_IsInvalid()
    {
        var cmd = ValidAddDocCommand() with { FileUrl = "" };
        var result = AddDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddDocValidator_EmptyFileName_IsInvalid()
    {
        var cmd = ValidAddDocCommand() with { FileName = "" };
        var result = AddDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddDocValidator_FileSizeZero_IsInvalid()
    {
        var cmd = ValidAddDocCommand() with { FileSizeBytes = 0 };
        var result = AddDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AddDocValidator_FileSizeExceeds10MB_IsInvalid()
    {
        var cmd = ValidAddDocCommand() with { FileSizeBytes = 10 * 1024 * 1024 + 1 };
        var result = AddDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse("file size must not exceed 10 MB");
    }

    [Fact]
    public void AddDocValidator_InvalidDocumentType_IsInvalid()
    {
        var cmd = ValidAddDocCommand() with { DocumentType = (DocumentType)99 };
        var result = AddDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ReplaceProviderDocumentCommandValidator
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly ReplaceProviderDocumentCommandValidator ReplaceDocValidator = new();

    private static ReplaceProviderDocumentCommand ValidReplaceDocCommand() => new(
        DocumentId:    Guid.NewGuid(),
        FileUrl:       "https://storage.example.com/docs/new-license.pdf",
        FileName:      "new-license.pdf",
        FileSizeBytes: 1024 * 256,
        ExpiresAt:     null);

    [Fact]
    public void ReplaceDocValidator_ValidCommand_IsValid()
    {
        var result = ReplaceDocValidator.Validate(ValidReplaceDocCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReplaceDocValidator_EmptyDocumentId_IsInvalid()
    {
        var cmd = ValidReplaceDocCommand() with { DocumentId = Guid.Empty };
        var result = ReplaceDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ReplaceDocValidator_EmptyFileUrl_IsInvalid()
    {
        var cmd = ValidReplaceDocCommand() with { FileUrl = "" };
        var result = ReplaceDocValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RejectProviderCommandValidator
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly RejectProviderCommandValidator RejectValidator = new();

    [Fact]
    public void RejectValidator_ValidCommand_IsValid()
    {
        var cmd = new RejectProviderCommand(Guid.NewGuid(), "Insufficient documentation provided.");
        var result = RejectValidator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RejectValidator_EmptyApplicationId_IsInvalid()
    {
        var cmd = new RejectProviderCommand(Guid.Empty, "Some reason");
        var result = RejectValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RejectValidator_EmptyReason_IsInvalid()
    {
        var cmd = new RejectProviderCommand(Guid.NewGuid(), "");
        var result = RejectValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // SuspendProviderCommandValidator
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly SuspendProviderCommandValidator SuspendValidator = new();

    [Fact]
    public void SuspendValidator_ValidCommand_IsValid()
    {
        var cmd = new SuspendProviderCommand(Guid.NewGuid(), "Document expired.");
        var result = SuspendValidator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void SuspendValidator_EmptyReason_IsInvalid()
    {
        var cmd = new SuspendProviderCommand(Guid.NewGuid(), "");
        var result = SuspendValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RequestMoreDocsCommandValidator
    // ──────────────────────────────────────────────────────────────────────────

    private static readonly RequestMoreDocsCommandValidator RequestDocsValidator = new();

    [Fact]
    public void RequestDocsValidator_ValidCommand_IsValid()
    {
        var cmd = new RequestMoreDocsCommand(
            ApplicationId:        Guid.NewGuid(),
            MissingDocumentTypes: [DocumentType.BusinessLicense, DocumentType.TaxRegistration],
            Notes:                "Please provide updated copies.");
        var result = RequestDocsValidator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RequestDocsValidator_EmptyDocumentTypes_IsInvalid()
    {
        var cmd = new RequestMoreDocsCommand(
            ApplicationId:        Guid.NewGuid(),
            MissingDocumentTypes: [],
            Notes:                "Notes here");
        var result = RequestDocsValidator.Validate(cmd);
        result.IsValid.Should().BeFalse("at least one missing document type must be specified");
    }

    [Fact]
    public void RequestDocsValidator_EmptyApplicationId_IsInvalid()
    {
        var cmd = new RequestMoreDocsCommand(
            ApplicationId:        Guid.Empty,
            MissingDocumentTypes: [DocumentType.GovernmentId],
            Notes:                "Valid notes");
        var result = RequestDocsValidator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}

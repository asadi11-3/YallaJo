using Accounts.Application.Commands.Admin.RejectProvider;
using Accounts.Application.Commands.Admin.RequestMoreDocs;
using Accounts.Application.Commands.Admin.SuspendProvider;
using Accounts.Application.Commands.Provider.RegisterProvider;
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

using System.Reflection;
using Accounts.Domain.Entities;
using Accounts.Domain.Enums;

namespace Accounts.Tests.Unit;

/// <summary>
/// Test helpers for the Agency handler tests.
///
/// Building a <see cref="ProviderApplication"/> in a specific (Type, Status)
/// combination through the public state machine requires type-specific document
/// sets plus Submit/Approve transitions. For guard-only handler tests we just
/// need an aggregate exposing the right <c>UserId</c>, <c>Type</c> and
/// <c>Status</c>, so we register a draft and set the two backing fields via
/// reflection — mirroring the reflection pattern already used in
/// <c>ProviderApplicationTests.SetCoolingPeriodEnded</c>.
/// </summary>
internal static class AgencyTestData
{
    public static ProviderApplication ProviderApplication(
        Guid userId,
        ProviderType type,
        ProviderApplicationStatus status)
    {
        var app = Accounts.Domain.Entities.ProviderApplication.Register(
            userId:               userId,
            type:                 type,
            businessName:         "Test Provider",
            contactEmail:         "test@example.com",
            contactPhone:         "+1234567890",
            address:              "1 Test Street",
            description:          "Test description",
            typeSpecificDataJson: null).Value;

        SetBackingField(app, "<Status>k__BackingField", status);
        return app;
    }

    private static void SetBackingField(object target, string backingFieldName, object value)
    {
        var field = target.GetType().GetField(
            backingFieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Backing field '{backingFieldName}' not found on {target.GetType().Name}.");

        field.SetValue(target, value);
    }
}

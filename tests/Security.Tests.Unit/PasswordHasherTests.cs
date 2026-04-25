using FluentAssertions;
using Security.Application.Interfaces;

namespace Security.Tests.Unit;

/// <summary>
/// Bug-fix coverage for <c>Security.Infrastructure.Services.PasswordHasher</c>.
/// <para>
/// Pins the contract that the wrapper NEVER throws on a malformed
/// stored hash — instead it returns <c>false</c>. Without this guard,
/// any user whose <c>PasswordHash</c> is the intentional non-Base64
/// placeholder set by external auto-create
/// (<c>EXTERNAL-ONLY:&lt;guid&gt;</c>) or Phase 3C admin reassignment
/// (<c>REASSIGNED:&lt;guid&gt;</c>) would crash local password login
/// with <see cref="FormatException"/> bubbling up as a 500.
/// </para>
/// </summary>
public sealed class PasswordHasherTests
{
    private static IPasswordHasher CreateSut()
    {
        // Reflection-based construction (same pattern as the
        // UserRegistrationServiceExternalTests fixture) since
        // PasswordHasher is internal sealed.
        var assembly = typeof(Security.Infrastructure.DependencyInjection).Assembly;
        var type = assembly.GetType(
            "Security.Infrastructure.Services.PasswordHasher",
            throwOnError: true)!;
        return (IPasswordHasher)Activator.CreateInstance(type)!;
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHash_IsExternalOnlyPlaceholder()
    {
        var sut = CreateSut();

        // Mirrors UserRegistrationService.RegisterExternalAsync:
        //   user.SetInitialPasswordHash("EXTERNAL-ONLY:" + Guid.NewGuid().ToString("N"));
        var placeholder = "EXTERNAL-ONLY:" + Guid.NewGuid().ToString("N");

        FluentActions.Invoking(() => sut.Verify("AnyPassword!", placeholder))
            .Should().NotThrow(
                "external-only users must never crash local password login");

        sut.Verify("AnyPassword!", placeholder).Should().BeFalse(
            "the placeholder is fail-closed by design — it must never grant access");
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHash_IsReassignedPlaceholder()
    {
        var sut = CreateSut();

        // Mirrors SecurityService.ReassignUserByAdminAsync:
        //   var placeholderHash = "REASSIGNED:" + Guid.NewGuid().ToString("N");
        var placeholder = "REASSIGNED:" + Guid.NewGuid().ToString("N");

        FluentActions.Invoking(() => sut.Verify("AnyPassword!", placeholder))
            .Should().NotThrow();

        sut.Verify("AnyPassword!", placeholder).Should().BeFalse();
    }

    [Theory]
    [InlineData("not!base64@@")]
    [InlineData("totally not a hash")]
    [InlineData("$2a$bcrypt-style-but-not-identity")]
    [InlineData("===")]
    public void Verify_ShouldReturnFalse_WhenHash_IsArbitraryNonBase64(string malformed)
    {
        var sut = CreateSut();

        FluentActions.Invoking(() => sut.Verify("AnyPassword!", malformed))
            .Should().NotThrow(
                "the wrapper must convert FormatException into a fail-closed Verify result");

        sut.Verify("AnyPassword!", malformed).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Verify_ShouldReturnFalse_WhenHash_IsNullOrEmptyOrWhitespace(string? blank)
    {
        var sut = CreateSut();

        sut.Verify("AnyPassword!", blank!).Should().BeFalse(
            "no stored credential ⇒ no possible match");
    }

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatches_ValidHash()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Pa55word!");

        sut.Verify("Pa55word!", hash).Should().BeTrue(
            "valid Identity hashes must continue to verify correctly");
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatch_ValidHash()
    {
        var sut = CreateSut();
        var hash = sut.Hash("Pa55word!");

        sut.Verify("WrongPassword!", hash).Should().BeFalse();
    }
}

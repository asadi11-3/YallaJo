using Finance.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace Finance.Tests.Unit;

public sealed class PaymentRedactorTests
{
    [Fact]
    public void Redact_ShouldMaskPan_KeepingFirstSixAndLastFour()
    {
        var input = "card=4242424242424242 paid";
        PaymentRedactor.Redact(input).Should().Contain("424242******4242");
    }

    [Fact]
    public void Redact_ShouldMaskCvvInJson()
    {
        var input = "{\"cvv\":\"123\"}";
        PaymentRedactor.Redact(input).Should().NotContain("123").And.Contain("***");
    }

    [Fact]
    public void Redact_NullOrEmpty_ReturnsEmpty()
    {
        PaymentRedactor.Redact(null).Should().BeEmpty();
        PaymentRedactor.Redact(string.Empty).Should().BeEmpty();
    }
}

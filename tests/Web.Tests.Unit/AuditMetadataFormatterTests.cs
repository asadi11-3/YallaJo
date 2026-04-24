using FluentAssertions;
using YallaJo.Web.Areas.Admin.Modules.Security.Features.AuditLogs.Helpers;

namespace Web.Tests.Unit;

/// <summary>
/// Phase 5A — covers <c>AuditMetadataFormatter</c>, the helper behind the
/// <c>_MetadataDetails</c> partial. Three branches matter: empty input,
/// valid JSON (must pretty-print), invalid JSON (must surface the raw
/// string for the view to HTML-encode safely).
/// </summary>
public sealed class AuditMetadataFormatterTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_WhenNullOrBlank_ReturnsDashAndIsEmpty(string? input)
    {
        var result = AuditMetadataFormatter.Format(input);

        result.IsEmpty.Should().BeTrue();
        result.IsValidJson.Should().BeFalse();
        result.Text.Should().Be("-");
    }

    [Fact]
    public void Format_ValidJson_ReturnsIndentedText_AndIsValidJson()
    {
        const string compact = "{\"oldEmail\":\"old@x.test\",\"newEmail\":\"new@x.test\",\"profileScrubbed\":true}";

        var result = AuditMetadataFormatter.Format(compact);

        result.IsEmpty.Should().BeFalse();
        result.IsValidJson.Should().BeTrue();

        // System.Text.Json's pretty-printer uses 2-space indentation by
        // default — assert key indentation/newline markers rather than
        // an exact byte-for-byte string so we don't pin a JSON format
        // detail.
        result.Text.Should().Contain("\n");
        result.Text.Should().Contain("\"oldEmail\": \"old@x.test\"");
        result.Text.Should().Contain("\"profileScrubbed\": true");
    }

    [Fact]
    public void Format_InvalidJson_ReturnsRawString_AndIsValidJsonFalse()
    {
        const string raw = "not really json — just a free-form note from a future writer";

        var result = AuditMetadataFormatter.Format(raw);

        result.IsEmpty.Should().BeFalse();
        result.IsValidJson.Should().BeFalse();
        result.Text.Should().Be(raw,
            "the raw string must be returned verbatim so the view can HTML-encode it");
    }

    [Fact]
    public void Format_TruncatedJson_FallsBackToRaw()
    {
        const string truncated = "{\"oldEmail\":\"a@b.test\""; // missing closing brace

        var result = AuditMetadataFormatter.Format(truncated);

        result.IsValidJson.Should().BeFalse();
        result.Text.Should().Be(truncated);
    }

    [Fact]
    public void Format_JsonArray_IsTreatedAsValidJson()
    {
        const string array = "[1,2,3]";

        var result = AuditMetadataFormatter.Format(array);

        result.IsValidJson.Should().BeTrue();
        result.Text.Should().Contain("1");
        result.Text.Should().Contain("\n");
    }
}

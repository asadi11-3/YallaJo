using Auth.Infrastructure.Services;
using FluentAssertions;

namespace Auth.Tests.Unit;

/// <summary>
/// Verifies the Gmail SMTP defaults reflect the real-world middle-box fix:
/// port 465 + SslOnConnect instead of 587 + STARTTLS. The 587 + STARTTLS path
/// was documented to hang on this environment because AV/VPN/ISP proxies
/// complete the TCP handshake locally but fail to forward the plaintext SMTP
/// banner. Implicit TLS on 465 immediately negotiates TLS as the first bytes,
/// which no transparent proxy can rewrite.
/// </summary>
public sealed class GmailOptionsTests
{
    [Fact]
    public void Defaults_ShouldPointToImplicitTlsOnPort465()
    {
        var opts = new GmailOptions();

        opts.Host.Should().Be("smtp.gmail.com");
        opts.Port.Should().Be(465, "STARTTLS on 587 hangs behind AV/VPN middle-boxes on multiple developer machines");
        opts.SecureSocketOptions.Should().Be("SslOnConnect",
            "implicit TLS starts the handshake on the client's first bytes, " +
            "so a transparent proxy cannot intercept or strip a plaintext banner");
        opts.TimeoutSeconds.Should().BeGreaterThan(0);
    }
}

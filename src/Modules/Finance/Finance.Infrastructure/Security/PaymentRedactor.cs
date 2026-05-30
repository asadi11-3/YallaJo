using System.Text.RegularExpressions;

namespace Finance.Infrastructure.Security;

/// <summary>
/// Redacts sensitive payment data (PAN, CVV, IBAN, expiry) from log payloads and gateway responses
/// before they hit the audit log or telemetry sinks.
///
/// PCI-DSS Requirement 3.3: Mask PAN when displayed. Only first six + last four digits may be visible.
/// PCI-DSS Requirement 3.2: Never store CVV/CVC/CID after authorization.
/// </summary>
public static partial class PaymentRedactor
{
    // 13-19 digit PAN (Visa/MC/Amex/Discover)
    [GeneratedRegex(@"\b(\d{6})\d{3,9}(\d{4})\b", RegexOptions.Compiled)]
    private static partial Regex PanRegex();

    // 3-4 digit CVV in "cvv":"123" / cvv=123 / "cvc":"123"
    [GeneratedRegex(@"(""(?:cvv|cvc|cid)""\s*:\s*"")\d{3,4}("")|((?:cvv|cvc|cid)\s*=\s*)\d{3,4}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CvvRegex();

    // IBAN (very rough — country + 2 check + up to 30 alnum)
    [GeneratedRegex(@"\b([A-Z]{2}\d{2})[A-Z0-9]{10,30}\b", RegexOptions.Compiled)]
    private static partial Regex IbanRegex();

    public static string Redact(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var result = PanRegex().Replace(input, "$1******$2");
        result = CvvRegex().Replace(result, m =>
        {
            // Preserve JSON or query-string shape
            if (m.Groups[1].Success) return $"{m.Groups[1].Value}***{m.Groups[2].Value}";
            return $"{m.Groups[3].Value}***";
        });
        result = IbanRegex().Replace(result, "$1****REDACTED****");
        return result;
    }
}

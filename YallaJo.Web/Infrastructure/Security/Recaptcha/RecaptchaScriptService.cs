using Microsoft.Extensions.Options;

namespace YallaJo.Web.Infrastructure.Security.Recaptcha;

/// <summary>
/// Small helper injected into layout views to render the reCAPTCHA v3 script
/// tag once per page and expose the site key. Keeping it behind an injectable
/// service means views never read the raw option class and the site key is
/// easy to stub in tests.
/// </summary>
public interface IRecaptchaScriptService
{
    bool IsEnabled { get; }
    string SiteKey { get; }
}

public sealed class RecaptchaScriptService : IRecaptchaScriptService
{
    public RecaptchaScriptService(IOptions<RecaptchaOptions> options)
    {
        var opts = options.Value;
        SiteKey = opts.SiteKey;
        IsEnabled = opts.IsEnabled;
    }

    public bool IsEnabled { get; }
    public string SiteKey { get; }
}

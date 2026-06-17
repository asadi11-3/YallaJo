using Microsoft.Data.SqlClient;

namespace YallaJo.Api.Extensions;

/// <summary>
/// Patch 0A — fail-fast validation of secrets and transport security at startup.
///
/// <para>
/// Secrets are no longer stored in <c>appsettings*.json</c>. They must be supplied
/// out-of-band: <c>dotnet user-secrets</c> locally, or environment variables /
/// a key vault in deployed environments. This guard turns a missing secret into a
/// clear, immediate startup failure instead of an obscure runtime error (or, worse,
/// a silently insecure configuration).
/// </para>
///
/// <para>
/// Scope is deliberately narrow to avoid false positives in tests and to avoid
/// duplicating the per-feature option validators that already exist
/// (Gmail, Azure Translator, reCAPTCHA, External Auth, ContentBlogs viewer hash).
/// This guard only asserts the secrets that are <b>always</b> required for the API
/// to function safely, plus the production transport-encryption invariant.
/// </para>
/// </summary>
public static class StartupSecretGuards
{
    private const string ConnectionStringName = "DefaultConnection";

    /// <summary>
    /// Validates required secrets and the production SQL transport-encryption
    /// invariant. Throws <see cref="InvalidOperationException"/> on the first
    /// violation so the application refuses to start misconfigured.
    /// </summary>
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        // 1. Always-required signing secrets (non-blank in every environment).
        RequireNonBlank(
            configuration["Jwt:Key"],
            key: "Jwt:Key",
            envVar: "Jwt__Key");

        RequireNonBlank(
            configuration["ExternalAuth:SigningKey"],
            key: "ExternalAuth:SigningKey",
            envVar: "ExternalAuth__SigningKey");

        // 2. Production-only: the SQL connection MUST enforce transport encryption.
        //    (Catches the Encrypt=False regression that shipped in V1.)
        if (environment.IsProduction())
        {
            RequireEncryptedSqlConnection(configuration);
        }
    }

    private static void RequireNonBlank(string? value, string key, string envVar)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required secret '{key}' is not configured. " +
                $"Supply it via user-secrets (dev) or the '{envVar}' environment variable (deployed).");
        }
    }

    private static void RequireEncryptedSqlConnection(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Supply it via the 'ConnectionStrings__DefaultConnection' environment variable (prod).");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is malformed and could not be parsed.", ex);
        }

        // SqlConnectionStringBuilder.Encrypt is a SqlConnectionEncryptOption.
        // Mandatory/Strict (or the legacy boolean "true") are acceptable; "false"/Optional is not.
        var encrypt = builder.Encrypt.ToString();
        var isEncrypted =
            encrypt.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            encrypt.Equals("Mandatory", StringComparison.OrdinalIgnoreCase) ||
            encrypt.Equals("Strict", StringComparison.OrdinalIgnoreCase);

        if (!isEncrypted)
        {
            throw new InvalidOperationException(
                "Production SQL connection must enforce transport encryption. " +
                "Set 'Encrypt=True' (or Strict) in 'ConnectionStrings__DefaultConnection'. " +
                $"Current Encrypt value: '{encrypt}'.");
        }
    }
}

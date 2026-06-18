using System.Net;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace YallaJo.Accounts.IntegrationTests;

/// <summary>
/// Patch B1 — verifies the static-file 404 short-circuit in <c>YallaJo.Api/Program.cs</c>
/// that runs BEFORE <c>app.UseStaticFiles()</c>.
///
/// The security guarantee under test: private provider-document PII placed under the
/// guarded upload folders is NEVER served anonymously by the static-file middleware,
/// even when the physical file actually exists on disk.
///
/// To make the proof meaningful, each test writes a REAL file into the host's web root
/// and then requests it directly over HTTP:
///   • A file under a guarded folder (/uploads/provider-documents, /uploads/provider-application-documents)
///     must return 404 — proving the guard blocks it.
///   • A POSITIVE CONTROL file with identical bytes under a NON-guarded /uploads subfolder
///     must return 200 — proving the static-file middleware is actually serving content,
///     so the 404 above is attributable to the guard and not to missing static files.
///
/// Requests are anonymous and do not follow redirects, so the status code is the raw
/// response of the static pipeline.
/// </summary>
public sealed class ProviderDocumentsStaticFileGuardTests
{
    private static readonly byte[] FileBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37]; // "%PDF-1.7"

    [Fact]
    public async Task Existing_file_under_provider_documents_folder_is_blocked_with_404()
    {
        using var factory = new StaticFileFactory();
        var webRoot = factory.ResolveWebRoot();

        // Write a REAL physical file under the guarded Booking folder.
        var fileName = $"b1-guard-{Guid.NewGuid():N}.pdf";
        var (guardedDir, guardedPath) = WriteProbeFile(webRoot, "provider-documents", fileName);

        try
        {
            using var client = factory.CreateClient(NoRedirect());

            using var response = await client.GetAsync($"/uploads/provider-documents/{fileName}");

            // The file exists on disk, yet the guard must return 404.
            response.StatusCode.Should().Be(
                HttpStatusCode.NotFound,
                "an existing file under /uploads/provider-documents must be blocked by the static-file guard");
        }
        finally
        {
            CleanupProbeFile(guardedPath, guardedDir);
        }
    }

    [Fact]
    public async Task Positive_control_file_under_non_guarded_uploads_folder_is_served_with_200()
    {
        using var factory = new StaticFileFactory();
        var webRoot = factory.ResolveWebRoot();

        // Same bytes, but under a NON-guarded uploads subfolder. If static files are
        // wired correctly this returns 200 — which proves the 404 in the guarded test
        // is the guard, not a missing-static-files environment problem.
        var probeFolder = $"__b1_probe_public_{Guid.NewGuid():N}";
        var fileName = "probe.pdf";
        var (publicDir, publicPath) = WriteProbeFile(webRoot, probeFolder, fileName);

        try
        {
            using var client = factory.CreateClient(NoRedirect());

            using var response = await client.GetAsync($"/uploads/{probeFolder}/{fileName}");

            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                "static-file middleware must serve files under non-guarded /uploads subfolders");
            var body = await response.Content.ReadAsByteArrayAsync();
            body.Should().Equal(FileBytes);
        }
        finally
        {
            CleanupProbeFile(publicPath, publicDir);
        }
    }

    [Fact]
    public async Task Existing_file_under_provider_application_documents_folder_is_blocked_with_404()
    {
        // Regression guard for the pre-existing Accounts folder protection — proves the
        // B1 edit (which added the Booking folder to the same short-circuit) did not
        // weaken the original Accounts guard.
        using var factory = new StaticFileFactory();
        var webRoot = factory.ResolveWebRoot();

        var fileName = $"b1-guard-{Guid.NewGuid():N}.pdf";
        var (guardedDir, guardedPath) = WriteProbeFile(webRoot, "provider-application-documents", fileName);

        try
        {
            using var client = factory.CreateClient(NoRedirect());

            using var response = await client.GetAsync($"/uploads/provider-application-documents/{fileName}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            CleanupProbeFile(guardedPath, guardedDir);
        }
    }

    [Fact]
    public async Task Sibling_folder_with_similar_prefix_is_not_accidentally_blocked()
    {
        // StartsWithSegments matches WHOLE segments, so "/uploads/provider-documents-public"
        // must NOT be caught by the "/uploads/provider-documents" guard.
        using var factory = new StaticFileFactory();
        var webRoot = factory.ResolveWebRoot();

        var folder = $"provider-documents-public-{Guid.NewGuid():N}";
        var fileName = "probe.pdf";
        var (dir, path) = WriteProbeFile(webRoot, folder, fileName);

        try
        {
            using var client = factory.CreateClient(NoRedirect());

            using var response = await client.GetAsync($"/uploads/{folder}/{fileName}");

            response.StatusCode.Should().Be(
                HttpStatusCode.OK,
                "whole-segment matching must not block sibling folders that merely share a prefix");
        }
        finally
        {
            CleanupProbeFile(path, dir);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static WebApplicationFactoryClientOptions NoRedirect()
        => new() { AllowAutoRedirect = false };

    private static (string dir, string path) WriteProbeFile(string webRoot, string uploadsSubfolder, string fileName)
    {
        var dir = Path.Combine(webRoot, "uploads", uploadsSubfolder);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        File.WriteAllBytes(path, FileBytes);
        return (dir, path);
    }

    private static void CleanupProbeFile(string path, string dir)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Only remove the directory if WE created it and it is now empty
            // (never remove a pre-existing populated uploads folder).
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }
        catch
        {
            // Best-effort cleanup; a leftover probe file is harmless (gitignored uploads).
        }
    }

    /// <summary>
    /// Minimal host factory for static-file pipeline tests. No auth or repository stubs
    /// are needed because the B1 guard runs before authentication and is exercised by
    /// anonymous requests. Test-only fake secrets satisfy the startup secret guards.
    /// </summary>
    private sealed class StaticFileFactory : WebApplicationFactory<Program>
    {
        private const string FakeJwtKey =
            "yallajo-test-only-fake-jwt-signing-key-do-not-use-in-production-0123456789";
        private const string FakeExternalAuthSigningKey =
            "yallajo-test-only-fake-externalauth-signing-key-do-not-use-in-production-0123456789";

        public string ResolveWebRoot()
        {
            using var scope = Services.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var webRoot = env.WebRootPath
                ?? Path.Combine(env.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(webRoot);
            return webRoot;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseSetting("Jwt:Key", FakeJwtKey);
            builder.UseSetting("ExternalAuth:SigningKey", FakeExternalAuthSigningKey);
        }
    }
}

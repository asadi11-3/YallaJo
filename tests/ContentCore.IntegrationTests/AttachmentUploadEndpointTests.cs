using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ContentCore.Application.Authorization;
using ContentCore.Application.Interfaces;
using ContentCore.Contracts.Authorization;
using ContentCore.Domain.Entities;
using ContentCore.Domain.Enums;
using ContentCore.Domain.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Storage;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Domain.Abstractions.Results;
using YallaJo.SharedKernel.Presentation.Authorization;
using Xunit;

namespace YallaJo.ContentCore.IntegrationTests;

/// <summary>
/// Real end-to-end binding test for <c>POST /api/v1/content-core/attachments/</c>.
///
/// Reproduces and pins down the staging-proxy Blocker #2 root causes:
///
/// <list type="bullet">
///   <item>
///     <b>Multipart form binding</b> — the Web ApiClient sends
///     EntityType/EntityId/AttachmentType/SortOrder as multipart <c>form</c>
///     fields. The endpoint must read them from the form, not the query string.
///     The earlier <c>[AsParameters]</c> binding silently failed and returned
///     400 with an empty body.
///   </item>
///   <item>
///     <b>FileSize=0 / empty storage payload</b> — the stream handed to the
///     storage service must carry the full file contents through magic-byte
///     detection and the subsequent <c>CopyToAsync</c>. The endpoint now
///     buffers the upload into a fully-seekable <see cref="MemoryStream"/>
///     before invoking the handler.
///   </item>
///   <item>
///     <b>Ownership guard</b> — the existing IDOR / target-entity guard must
///     keep rejecting non-owner uploads with 403 regardless of the binding
///     fix.
///   </item>
/// </list>
///
/// These tests exercise the real Kestrel pipeline + parameter binder by
/// driving the real API host through <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// We stub only the persistence and storage layers; routing, model binding,
/// authentication, authorization, FluentValidation, and the upload handler
/// itself all run unmodified.
/// </summary>
[Trait("Category", "blocker-fix")]
public sealed class AttachmentUploadEndpointTests : IClassFixture<UploadEndpointFactory>
{
    private readonly UploadEndpointFactory _factory;

    // Minimal real PNG with a valid signature so DetectFileTypeAsync passes.
    // Kept small to comfortably fit the per-type AttachmentLimits cap.
    private static readonly byte[] ValidPng = PngFixture.Build(width: 64, height: 64);

    public AttachmentUploadEndpointTests(UploadEndpointFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Upload_WithMultipartFormFields_PersistsAttachment_AndForwardsNonEmptyStreamToStorage()
    {
        var entityId = Guid.NewGuid();
        _factory.Reset();
        _factory.ConfigureOwnership(ownsEntity: true);

        using var client = _factory.CreateClient();
        using var content = BuildFormUpload(
            file: ValidPng,
            fileName: "fixture.png",
            contentType: "image/png",
            entityType: "Place",
            entityId: entityId,
            attachmentType: "Image",
            sortOrder: 0);

        var response = await client.PostAsync("/api/v1/content-core/attachments/", content);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "multipart form fields must bind to the endpoint; on failure callers see 400 with an empty body. " +
            $"Response body: {body}");

        // ── Bytes that reached storage are the full payload ────────────────
        _factory.LastUploadBytes.Should().NotBeNull();
        _factory.LastUploadBytes!.Length.Should().Be(ValidPng.Length,
            "the endpoint must buffer the IFormFile so the storage layer sees the full payload " +
            "— staging proxy reproduced FileSize=0 because the stream was already consumed by magic-byte detection");
        _factory.LastUploadBytes!.Should().Equal(ValidPng,
            "the bytes that reach storage must equal the bytes that were uploaded; " +
            "this guards against any stream position drift between DetectFileTypeAsync and storage CopyToAsync");

        // ── Multipart form metadata bound and reached the domain entity ────
        _factory.LastAttachment.Should().NotBeNull("the handler must have persisted an Attachment");
        _factory.LastAttachment!.EntityType.Should().Be(EntityType.Place);
        _factory.LastAttachment!.EntityId.Should().Be(entityId);
        _factory.LastAttachment!.Type.Should().Be(AttachmentType.Image);
        _factory.LastAttachment!.OriginalFileName.Should().Be("fixture.png");
        _factory.LastAttachment!.MimeType.Should().Be("image/png");
        _factory.LastAttachment!.FileSize.Should().Be(ValidPng.Length,
            "FileSize must equal the actual bytes written by storage (regression for FileSize=0)");
        _factory.LastAttachment!.SortOrder.Should().Be(0);
    }

    [Fact]
    public async Task Upload_WithMultipartFormFields_NonOwner_Returns403_AndNeverInvokesStorage()
    {
        var entityId = Guid.NewGuid();
        _factory.Reset();
        _factory.ConfigureOwnership(ownsEntity: false);

        using var client = _factory.CreateClient();
        using var content = BuildFormUpload(
            file: ValidPng,
            fileName: "fixture.png",
            contentType: "image/png",
            entityType: "Place",
            entityId: entityId,
            attachmentType: "Image",
            sortOrder: 0);

        var response = await client.PostAsync("/api/v1/content-core/attachments/", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.LastUploadBytes.Should().BeNull(
            "the file storage service must NOT be invoked when ownership is refused " +
            "(otherwise an orphan file leaks to disk)");
        _factory.LastAttachment.Should().BeNull(
            "no Attachment row must be created when ownership is refused");
    }

    [Fact]
    public async Task Upload_WithMissingFile_Returns400()
    {
        _factory.Reset();
        _factory.ConfigureOwnership(ownsEntity: true);

        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new StringContent("Place"),                    "EntityType" },
            { new StringContent(Guid.NewGuid().ToString()),   "EntityId" },
            { new StringContent("Image"),                    "AttachmentType" },
            { new StringContent("0"),                        "SortOrder" },
        };

        var response = await client.PostAsync("/api/v1/content-core/attachments/", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.LastUploadBytes.Should().BeNull();
    }

    [Fact]
    public async Task BulkUpload_AcceptsMultipartFormMetadata_AndForwardsNonEmptyStreamsForEachFile()
    {
        var entityId = Guid.NewGuid();
        _factory.Reset();
        _factory.ConfigureOwnership(ownsEntity: true);

        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new StringContent("Place"),               "EntityType" },
            { new StringContent(entityId.ToString()),    "EntityId" },
        };
        var first = new ByteArrayContent(ValidPng);
        first.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(first, "files", "bulk-a.png");
        var second = new ByteArrayContent(ValidPng);
        second.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(second, "files", "bulk-b.png");

        var response = await client.PostAsync("/api/v1/content-core/attachments/images", content);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);

        _factory.UploadCallCount.Should().Be(2,
            "each file in the bulk upload must reach the storage service exactly once");
        _factory.AllUploadBytes.Should().AllSatisfy(bytes =>
            bytes.Length.Should().Be(ValidPng.Length,
                "every bulk-uploaded file must be buffered before reaching storage"));
    }

    private static MultipartFormDataContent BuildFormUpload(
        byte[] file, string fileName, string contentType,
        string entityType, Guid entityId, string attachmentType, int sortOrder)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(entityType),           "EntityType" },
            { new StringContent(entityId.ToString()),   "EntityId" },
            { new StringContent(attachmentType),       "AttachmentType" },
            { new StringContent(sortOrder.ToString()), "SortOrder" },
        };
        var fileContent = new ByteArrayContent(file);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);
        return form;
    }
}

/// <summary>
/// WebApplicationFactory that:
///   <list type="bullet">
///     <item>Forces ASPNETCORE_ENVIRONMENT=Testing so the unconditional
///       startup seeding does not try to connect to SQL Server.</item>
///     <item>Replaces every auth scheme with a test scheme that injects the
///       <c>Permission.Attachment.Create</c> + <c>Permission.Attachment.Read</c>
///       claims so the Authorization pipeline reaches the endpoint.</item>
///     <item>Stubs <see cref="IFileStorageService"/> to capture the bytes the
///       upload pipeline would have written, without touching disk.</item>
///     <item>Stubs <see cref="IAttachmentRepository"/> and the
///       UnitOfWork so we can assert the persisted Attachment entity without
///       a real database.</item>
///     <item>Stubs <see cref="IOwnershipGuard"/> so we can drive both the
///       owner success path AND the non-owner 403 path through the same
///       endpoint without seeding entity rows.</item>
///   </list>
/// </summary>
public sealed class UploadEndpointFactory : WebApplicationFactory<Program>
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly List<byte[]> _uploadBytes = new();
    private readonly List<Attachment> _persistedAttachments = new();
    private bool _ownsEntity = true;

    public byte[]? LastUploadBytes => _uploadBytes.Count == 0 ? null : _uploadBytes[^1];
    public IReadOnlyList<byte[]> AllUploadBytes => _uploadBytes;
    public Attachment? LastAttachment => _persistedAttachments.Count == 0 ? null : _persistedAttachments[^1];
    public int UploadCallCount => _uploadBytes.Count;

    public void Reset()
    {
        _uploadBytes.Clear();
        _persistedAttachments.Clear();
        _ownsEntity = true;
    }

    public void ConfigureOwnership(bool ownsEntity) => _ownsEntity = ownsEntity;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Seeding:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            // ── Auth: replace every scheme with our test scheme ────────────
            services.AddAuthentication(TestPermissionAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestPermissionAuthHandler>(
                    TestPermissionAuthHandler.SchemeName, _ => { });

            services.PostConfigureAll<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestPermissionAuthHandler.SchemeName;
                o.DefaultChallengeScheme    = TestPermissionAuthHandler.SchemeName;
                o.DefaultScheme             = TestPermissionAuthHandler.SchemeName;
                o.DefaultForbidScheme       = TestPermissionAuthHandler.SchemeName;
            });

            // ── IOwnershipGuard: drive owner success + non-owner 403 ───────
            services.RemoveAll<IOwnershipGuard>();
            services.AddSingleton<IOwnershipGuard>(_ =>
                new ToggleableOwnershipGuard(() => _ownsEntity));

            // ── IFileStorageService: capture exact bytes that reach storage ─
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService>(_ => new CapturingFileStorage(_uploadBytes));

            // ── IAttachmentRepository: capture the persisted Attachment ────
            services.RemoveAll<IAttachmentRepository>();
            services.AddSingleton<IAttachmentRepository>(_ =>
            {
                var repo = Substitute.For<IAttachmentRepository>();
                // Only methods actually used by UploadAttachmentCommandHandler need
                // real behaviour; the rest of the IRead/IWriteRepository surface is
                // left as no-op NSubstitute defaults.
                repo.CountByEntityAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(call => Task.FromResult(_persistedAttachments.Count(a =>
                        a.EntityType == call.Arg<EntityType>() && a.EntityId == call.Arg<Guid>())));
                repo.AddAsync(Arg.Any<Attachment>(), Arg.Any<CancellationToken>())
                    .Returns(call =>
                    {
                        _persistedAttachments.Add(call.Arg<Attachment>());
                        return Task.CompletedTask;
                    });
                return repo;
            });

            // ── UnitOfWork: no-op (we don't have a real DB) ────────────────
            services.RemoveAll<IContentCoreUnitOfWork>();
            services.AddSingleton<IContentCoreUnitOfWork>(_ =>
            {
                var uow = Substitute.For<IContentCoreUnitOfWork>();
                uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
                return uow;
            });

            // ── Media-processing queue: no-op ──────────────────────────────
            services.RemoveAll<IMediaProcessingQueue>();
            services.AddSingleton<IMediaProcessingQueue>(_ =>
            {
                var q = Substitute.For<IMediaProcessingQueue>();
                q.EnqueueAsync(Arg.Any<MediaProcessingJob>(), Arg.Any<CancellationToken>())
                    .Returns(ValueTask.CompletedTask);
                return q;
            });
        });
    }

    /// <summary>
    /// Auth scheme that injects exactly the permission claims required by the
    /// attachment endpoints so the Authorization pipeline can be exercised.
    /// </summary>
    private sealed class TestPermissionAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ContentCoreIntegrationTestScheme";

        public TestPermissionAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, TestUserId.ToString()),
                new("sub", TestUserId.ToString()),
                new("Permission",
                    PermissionPolicyNames.Build(ContentCoreFeatures.Attachment, AppAction.Create)),
                new("Permission",
                    PermissionPolicyNames.Build(ContentCoreFeatures.Attachment, AppAction.Read)),
            };

            var identity  = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket    = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class ToggleableOwnershipGuard : IOwnershipGuard
    {
        private readonly Func<bool> _allow;
        public ToggleableOwnershipGuard(Func<bool> allow) => _allow = allow;
        public bool IsAdminTier => false;
        public Task<Result> AuthorizeAsync(
            EntityType entityType, Guid entityId, string errorPrefix,
            string? forbiddenMessage = null, CancellationToken ct = default)
        {
            return Task.FromResult(_allow()
                ? Result.Success()
                : Result.Failure(
                    new Error($"{errorPrefix}.Forbidden", forbiddenMessage ?? "Forbidden."),
                    Outcome.Forbidden));
        }
    }

    private sealed class CapturingFileStorage : IFileStorageService
    {
        private readonly List<byte[]> _capture;
        public CapturingFileStorage(List<byte[]> capture) => _capture = capture;

        public async Task<Result<FileUploadResult>> UploadAsync(
            Stream stream, string fileName, string contentType, string folder, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();
            _capture.Add(bytes);
            return Result<FileUploadResult>.Success(new FileUploadResult(
                Url: $"/uploads/{folder}/{Guid.NewGuid()}.bin",
                StorageKey: $"{folder}/{Guid.NewGuid()}.bin",
                FileSize: bytes.Length));
        }

        public Task<bool> DeleteAsync(string fileUrl, CancellationToken ct = default)
            => Task.FromResult(true);
        public Task<string> GetAccessUrlAsync(string fileUrl, TimeSpan? expiry = null, CancellationToken ct = default)
            => Task.FromResult(fileUrl);
    }

}

internal static class ServiceCollectionRemoveAllExt
{
    public static void RemoveAll<TService>(this IServiceCollection services)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
                services.RemoveAt(i);
        }
    }
}

/// <summary>
/// Tiny hand-assembled PNG. We don't pull in an image package so the test
/// stays focused on the binding fix. The signature matches what
/// <c>UploadAttachmentCommandHandler.DetectFileTypeAsync</c> looks for.
/// </summary>
internal static class PngFixture
{
    public static byte[] Build(int width, int height)
    {
        var signature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var ihdr = BuildIhdr(width, height);
        var raw = new byte[height * (1 + width * 3)];
        var idat = BuildIdat(raw);
        var iend = Chunk("IEND", Array.Empty<byte>());

        using var ms = new MemoryStream();
        ms.Write(signature);
        ms.Write(ihdr);
        ms.Write(idat);
        ms.Write(iend);
        return ms.ToArray();
    }

    private static byte[] BuildIhdr(int w, int h)
    {
        var data = new byte[13];
        data[0] = (byte)(w >> 24); data[1] = (byte)(w >> 16); data[2] = (byte)(w >> 8); data[3] = (byte)w;
        data[4] = (byte)(h >> 24); data[5] = (byte)(h >> 16); data[6] = (byte)(h >> 8); data[7] = (byte)h;
        data[8] = 8;   // bit depth
        data[9] = 2;   // colour type = truecolour RGB
        data[10] = 0;  // compression
        data[11] = 0;  // filter
        data[12] = 0;  // interlace
        return Chunk("IHDR", data);
    }

    private static byte[] BuildIdat(byte[] raw)
    {
        using var deflated = new MemoryStream();
        // zlib wrapper
        deflated.WriteByte(0x78);
        deflated.WriteByte(0x9C);
        using (var deflate = new System.IO.Compression.DeflateStream(
            deflated, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(raw, 0, raw.Length);
        }
        var (s1, s2) = (1u, 0u);
        foreach (var b in raw)
        {
            s1 = (s1 + b) % 65521;
            s2 = (s2 + s1) % 65521;
        }
        var adler = (s2 << 16) | s1;
        deflated.WriteByte((byte)(adler >> 24));
        deflated.WriteByte((byte)(adler >> 16));
        deflated.WriteByte((byte)(adler >> 8));
        deflated.WriteByte((byte)adler);
        return Chunk("IDAT", deflated.ToArray());
    }

    private static byte[] Chunk(string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var len = data.Length;
        using var ms = new MemoryStream();
        ms.WriteByte((byte)(len >> 24));
        ms.WriteByte((byte)(len >> 16));
        ms.WriteByte((byte)(len >> 8));
        ms.WriteByte((byte)len);
        ms.Write(typeBytes);
        ms.Write(data);
        var crc = Crc32(typeBytes.Concat(data).ToArray());
        ms.WriteByte((byte)(crc >> 24));
        ms.WriteByte((byte)(crc >> 16));
        ms.WriteByte((byte)(crc >> 8));
        ms.WriteByte((byte)crc);
        return ms.ToArray();
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }
}

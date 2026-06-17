using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ContentCore.Application.Authorization;
using ContentCore.Application.Interfaces;
using ContentCore.Application.Queries.Attachment.GetAttachmentById;
using ContentCore.Application.Queries.Attachment.GetEntityAttachments;
using ContentCore.Contracts.Attachments;
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
using Security.Contracts.Authorization;
using YallaJo.SharedKernel.Application.Authorization;
using YallaJo.SharedKernel.Presentation.Authorization;
using Xunit;

namespace YallaJo.ContentCore.IntegrationTests;

/// <summary>
/// Patch 1B — read-side IDOR regression tests for the generic attachment read
/// endpoints:
///
/// <list type="bullet">
///   <item><c>GET /api/v1/content-core/attachments/{id:guid}</c> (GetAttachmentById)</item>
///   <item><c>GET /api/v1/content-core/attachments?entityType=&amp;entityId=</c> (GetEntityAttachments)</item>
/// </list>
///
/// Both endpoints already require <c>Permission.Attachment.Read</c> (anonymous → 401).
/// Before Patch 1B the handlers performed NO ownership check, so any authenticated
/// caller holding that coarse permission could read ANY attachment / any entity's
/// attachments by GUID (IDOR / draft leak). Patch 1B injects the real
/// <see cref="IOwnershipGuard"/> into both handlers and maps every denial to
/// <c>404 NotFound</c> (anti-enumeration).
///
/// <para>
/// These tests drive the REAL <c>OwnershipGuard</c> + REAL claims-based
/// <c>ICurrentUser</c>, substituting only:
/// </para>
/// <list type="bullet">
///   <item><see cref="IEntityOwnershipResolver"/> — returns a known
///     <see cref="EntityOwnershipResolution"/> with a fixed <c>OwnerUserId</c>,
///     so owner-equality and admin-tier bypass are exercised truthfully.</item>
///   <item><see cref="IAttachmentRepository"/> — returns a known
///     <see cref="Attachment"/> for the id/entity under test (no real DB).</item>
/// </list>
///
/// The public published tour/blog image path (<c>PublicEntityImageReader</c>) is a
/// SEPARATE handler/endpoint and is deliberately untouched by Patch 1B — see the
/// documentation-only fact asserted in
/// <see cref="PublicImagePath_IsServedBySeparateReader_NotByGuardedManagementEndpoints"/>.
/// </summary>
[Trait("Category", "idor-fix")]
public sealed class AttachmentReadAuthorizationTests
{
    // Fixed identities used across the tests.
    private static readonly Guid OwnerUserId = Guid.Parse("0a0a0a0a-0000-0000-0000-000000000001");
    private static readonly Guid OtherUserId = Guid.Parse("0b0b0b0b-0000-0000-0000-000000000002");
    private static readonly Guid AdminUserId = Guid.Parse("0c0c0c0c-0000-0000-0000-000000000003");

    private static readonly Guid KnownEntityId = Guid.Parse("11110000-0000-0000-0000-000000000001");
    private static readonly Guid KnownAttachmentId = Guid.Parse("22220000-0000-0000-0000-000000000002");

    private const string ByIdRoute = "/api/v1/content-core/attachments/22220000-0000-0000-0000-000000000002";
    private const string ByEntityRoute =
        "/api/v1/content-core/attachments?entityType=Tour&entityId=11110000-0000-0000-0000-000000000001";

    // ── GetAttachmentById ───────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Owner_CanReadOwnAttachment_200()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: OwnerUserId, isAdmin: false, ownerUserId: OwnerUserId);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByIdRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(KnownAttachmentId.ToString());
    }

    [Fact]
    public async Task GetById_CrossUser_IsDenied_As404_NotForbidden()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: OtherUserId, isAdmin: false, ownerUserId: OwnerUserId);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByIdRoute);

        // 404 (not 403) so a non-owner cannot distinguish "exists but forbidden"
        // from "does not exist" → prevents attachment existence enumeration.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Anonymous_IsDenied_401()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: OwnerUserId, isAdmin: false, ownerUserId: OwnerUserId,
            authenticate: false);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByIdRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_Admin_CanReadAnyAttachment_200()
    {
        // Admin caller is NOT the owner, but admin-tier bypass in OwnershipGuard
        // grants access.
        await using var factory = new ReadAuthFactory(
            callerUserId: AdminUserId, isAdmin: true, ownerUserId: OwnerUserId);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByIdRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(KnownAttachmentId.ToString());
    }

    // ── GetEntityAttachments ────────────────────────────────────────────────

    [Fact]
    public async Task GetByEntity_Owner_CanListOwnEntityAttachments_200()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: OwnerUserId, isAdmin: false, ownerUserId: OwnerUserId);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByEntityRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(KnownAttachmentId.ToString());
    }

    [Fact]
    public async Task GetByEntity_CrossUser_IsDenied_As404()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: OtherUserId, isAdmin: false, ownerUserId: OwnerUserId);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByEntityRoute);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByEntity_Anonymous_IsDenied_401()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: OwnerUserId, isAdmin: false, ownerUserId: OwnerUserId,
            authenticate: false);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByEntityRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetByEntity_Admin_CanListAnyEntityAttachments_200()
    {
        await using var factory = new ReadAuthFactory(
            callerUserId: AdminUserId, isAdmin: true, ownerUserId: OwnerUserId);
        var client = factory.CreateClient();

        var response = await client.GetAsync(ByEntityRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Public-image-path separation (documentation-pinning) ─────────────────

    [Fact]
    public void PublicImagePath_IsServedBySeparateReader_NotByGuardedManagementEndpoints()
    {
        // Patch 1B intentionally only guards the generic management read handlers
        // (GetAttachmentById / GetEntityAttachments). Published tour/blog images
        // are surfaced through the dedicated IPublicEntityImageReader projection,
        // which is a different type and a different (public) code path. This test
        // pins that separation so the public path is not accidentally folded into
        // the guarded handlers in a later refactor.
        var publicReader = typeof(IPublicEntityImageReader);
        var guardedByIdHandler = typeof(GetAttachmentByIdQueryHandler);
        var guardedByEntityHandler = typeof(GetEntityAttachmentsQueryHandler);

        publicReader.Should().NotBe(guardedByIdHandler);
        publicReader.Should().NotBe(guardedByEntityHandler);
        publicReader.Namespace.Should().NotBe(guardedByIdHandler.Namespace);
    }

    // ── Host factory ─────────────────────────────────────────────────────────

    /// <summary>
    /// Boots the real API host but substitutes the entity-ownership resolver and
    /// attachment repository so we can drive owner / cross-user / admin paths
    /// through the REAL OwnershipGuard. The caller identity (and admin role) is
    /// injected by a configurable test auth scheme.
    /// </summary>
    private sealed class ReadAuthFactory : WebApplicationFactory<Program>
    {
        private readonly Guid _callerUserId;
        private readonly bool _isAdmin;
        private readonly Guid _ownerUserId;
        private readonly bool _authenticate;

        public ReadAuthFactory(Guid callerUserId, bool isAdmin, Guid ownerUserId, bool authenticate = true)
        {
            _callerUserId = callerUserId;
            _isAdmin = isAdmin;
            _ownerUserId = ownerUserId;
            _authenticate = authenticate;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Seeding:Enabled", "false");
            builder.UseFakeTestSecrets();

            builder.ConfigureTestServices(services =>
            {
                // ── Auth: configurable caller identity + optional admin role ──
                services.AddAuthentication(ConfigurableTestAuthHandler.SchemeName)
                    .AddScheme<ConfigurableTestAuthOptions, ConfigurableTestAuthHandler>(
                        ConfigurableTestAuthHandler.SchemeName,
                        o =>
                        {
                            o.CallerUserId = _callerUserId;
                            o.IsAdmin = _isAdmin;
                            o.Authenticate = _authenticate;
                        });

                services.PostConfigureAll<AuthenticationOptions>(o =>
                {
                    o.DefaultAuthenticateScheme = ConfigurableTestAuthHandler.SchemeName;
                    o.DefaultChallengeScheme = ConfigurableTestAuthHandler.SchemeName;
                    o.DefaultScheme = ConfigurableTestAuthHandler.SchemeName;
                    o.DefaultForbidScheme = ConfigurableTestAuthHandler.SchemeName;
                });

                // ── IEntityOwnershipResolver: known owner for the target ──────
                // Keep the REAL OwnershipGuard registered; only swap the resolver
                // so owner-equality and admin-tier bypass run for real.
                services.RemoveAll<IEntityOwnershipResolver>();
                services.AddSingleton<IEntityOwnershipResolver>(_ =>
                {
                    var resolver = Substitute.For<IEntityOwnershipResolver>();
                    resolver
                        .ResolveAsync(Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new EntityOwnershipResolution(
                            IsSupported: true,
                            Exists: true,
                            IsDeleted: false,
                            OwnerUserId: _ownerUserId)));
                    return resolver;
                });

                // ── IAttachmentRepository: known attachment, no real DB ───────
                services.RemoveAll<IAttachmentRepository>();
                services.AddSingleton<IAttachmentRepository>(_ =>
                {
                    var repo = Substitute.For<IAttachmentRepository>();

                    var known = Attachment.Create(
                        entityType: EntityType.Tour,
                        entityId: KnownEntityId,
                        type: AttachmentType.Image,
                        url: "/uploads/tours/known-attachment.png",
                        uploadedByUserId: OwnerUserId,
                        originalFileName: "known.png",
                        mimeType: "image/png",
                        fileSize: 1024,
                        sortOrder: 0);

                    // Force the deterministic id used by the route under test.
                    SetAttachmentId(known, KnownAttachmentId);

                    repo.GetByIdAsync(KnownAttachmentId, Arg.Any<CancellationToken>(), Arg.Any<bool>())
                        .Returns(Task.FromResult<Attachment?>(known));

                    repo.GetAllAsync(
                            Arg.Any<System.Linq.Expressions.Expression<Func<Attachment, bool>>?>(),
                            Arg.Any<Func<IQueryable<Attachment>, IQueryable<Attachment>>?>(),
                            Arg.Any<Func<IQueryable<Attachment>, IOrderedQueryable<Attachment>>?>(),
                            Arg.Any<bool>(),
                            Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult(new List<Attachment> { known }));

                    repo.GetEntityImagesAsync(
                            Arg.Any<EntityType>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                        .Returns(Task.FromResult<IReadOnlyList<EntityImage>>(new List<EntityImage>()));

                    return repo;
                });
            });
        }

        // Attachment.Id comes from BaseEntity (Guid.CreateVersion7()) with a
        // private setter; reflect the backing property so the route id matches.
        private static void SetAttachmentId(Attachment attachment, Guid id)
        {
            var prop = typeof(Attachment).GetProperty(nameof(Attachment.Id))
                ?? throw new InvalidOperationException("Attachment.Id property not found.");
            prop.SetValue(attachment, id);
        }
    }

    private sealed class ConfigurableTestAuthOptions : AuthenticationSchemeOptions
    {
        public Guid CallerUserId { get; set; }
        public bool IsAdmin { get; set; }
        public bool Authenticate { get; set; } = true;
    }

    private sealed class ConfigurableTestAuthHandler : AuthenticationHandler<ConfigurableTestAuthOptions>
    {
        public const string SchemeName = "ContentCoreReadAuthTestScheme";

        public ConfigurableTestAuthHandler(
            IOptionsMonitor<ConfigurableTestAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Options.Authenticate)
            {
                // No identity → RequireAuthorization() challenges → 401.
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Options.CallerUserId.ToString()),
                new("sub", Options.CallerUserId.ToString()),
                new("Permission",
                    PermissionPolicyNames.Build(ContentCoreFeatures.Attachment, AppAction.Read)),
            };

            if (Options.IsAdmin)
            {
                // CurrentUser.Roles reads "role" ∪ ClaimTypes.Role; AppRoles.Admin
                // privilege level satisfies OwnershipGuard.IsAdminTier.
                claims.Add(new Claim("role", AppRoles.Admin));
                claims.Add(new Claim(ClaimTypes.Role, AppRoles.Admin));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

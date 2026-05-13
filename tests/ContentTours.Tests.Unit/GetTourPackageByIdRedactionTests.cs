using ContentTours.Application.Caching;
using ContentTours.Application.Queries.TourPackage.Common;
using ContentTours.Application.Queries.TourPackage.GetTourPackageById;
using ContentTours.Domain.Entities;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Domain.ValueObjects;

namespace ContentTours.Tests.Unit;

/// <summary>
/// P1-007 regression tests — clean public DTO design.
///
/// <para>
/// <c>GET /packages/{id}</c> is an <c>AllowAnonymous</c> public read endpoint and
/// must return a clean public DTO that does NOT carry sensitive / internal
/// fields:
/// </para>
///
/// <list type="bullet">
///   <item><c>CreatedByUserId</c> — must be absent from the public DTO</item>
///   <item><c>RowVersion</c>      — must be absent from the public DTO</item>
/// </list>
///
/// <para>
/// These tests pin the DTO shape and the handler projection so an admin/internal
/// field can never silently leak onto the public response again.  Cache key
/// elevation logic was deliberately removed in this redesign — the public
/// response is identical for every caller, so no elevation/admin dimension is
/// needed in the cache key.
/// </para>
///
/// <para>
/// If admin/management screens later need <c>CreatedByUserId</c> or
/// <c>RowVersion</c>, a separate **protected** management query / DTO must be
/// introduced — admin/internal fields must NOT be retro-fitted onto this
/// public DTO.
/// </para>
/// </summary>
public sealed class GetTourPackageByIdRedactionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GetTourPackageByIdQueryHandler BuildHandler(TourPackage? package)
    {
        var repo = Substitute.For<ITourPackageRepository>();
        repo.GetByIdWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(package);

        var logger = Substitute.For<ILogger<GetTourPackageByIdQueryHandler>>();
        return new GetTourPackageByIdQueryHandler(repo, logger);
    }

    private static TourPackage BuildPackage(Guid creatorId) =>
        TourPackage.Create(
            name:                  "Test Package",
            description:           null,
            price:                 new Money(99m, "JOD"),
            currency:              "JOD",
            maxParticipants:       null,
            validFrom:             null,
            validTo:               null,
            createdByUserId:       creatorId,
            includedTourIds:       new[] { Guid.NewGuid(), Guid.NewGuid() },
            inclusionDescriptions: Array.Empty<string>());

    // ── 1. DTO shape: CreatedByUserId is not declared on the public DTO ──────

    [Fact]
    public void TourPackageDetailDto_DoesNotDeclareCreatedByUserId()
    {
        typeof(TourPackageDetailDto)
            .GetProperty(nameof(TourPackage.CreatedByUserId))
            .Should().BeNull(
                "CreatedByUserId must be removed from the public TourPackageDetailDto. " +
                "Re-introducing it would leak it from the AllowAnonymous endpoint. " +
                "If admin/management screens need it, add a separate protected DTO (P1-007).");
    }

    // ── 2. DTO shape: RowVersion is not declared on the public DTO ───────────

    [Fact]
    public void TourPackageDetailDto_DoesNotDeclareRowVersion()
    {
        typeof(TourPackageDetailDto)
            .GetProperty("RowVersion")
            .Should().BeNull(
                "RowVersion (the optimistic-concurrency token) must be removed from the " +
                "public TourPackageDetailDto. It is an admin/management concern and must " +
                "be served via a future protected management query (P1-007).");
    }

    // ── 3. Handler projection: CreatedByUserId never reaches the response ────

    [Fact]
    public async Task GetTourPackageById_DoesNotExposeCreatedByUserId()
    {
        var creatorId = Guid.NewGuid();
        var package   = BuildPackage(creatorId);
        var handler   = BuildHandler(package);

        var query  = new GetTourPackageByIdQuery(package.Id, AcceptLanguage: null);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // The DTO does not declare a CreatedByUserId property in the first place,
        // so the only way to assert "not exposed" is to confirm the property is
        // absent from the projected response object.
        var dtoType = result.Value.GetType();
        dtoType.GetProperty("CreatedByUserId").Should().BeNull(
            "the handler must not project CreatedByUserId onto the public DTO (P1-007)");
    }

    // ── 4. Handler projection: RowVersion never reaches the response ─────────

    [Fact]
    public async Task GetTourPackageById_DoesNotExposeRowVersion()
    {
        var package = BuildPackage(Guid.NewGuid());
        var handler = BuildHandler(package);

        var query  = new GetTourPackageByIdQuery(package.Id, AcceptLanguage: null);
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var dtoType = result.Value.GetType();
        dtoType.GetProperty("RowVersion").Should().BeNull(
            "the handler must not project RowVersion onto the public DTO (P1-007)");
    }

    // ── 5. Cache key: independent of elevation ────────────────────────────────

    [Fact]
    public void PackageCacheKey_DoesNotDependOnElevation()
    {
        var packageId      = Guid.NewGuid();
        const string? lang = "en";

        // The query has a single canonical cache key — it deliberately does not
        // accept an elevation/admin flag, because the public DTO is identical for
        // every caller.  Removing the elevation dimension also removes the risk
        // of cross-slot contamination that the rejected design tried to mitigate.
        var key1 = new GetTourPackageByIdQuery(packageId, lang).CacheKey;
        var key2 = new GetTourPackageByIdQuery(packageId, lang).CacheKey;

        key1.Should().Be(key2, "the public cache key must be stable for identical inputs");

        key1.Should().Be(
            ContentToursCacheKeys.Package(packageId, lang),
            "the cache key must match the canonical ContentToursCacheKeys.Package helper");

        // Stable key format regression guard — must NOT contain an elevation suffix.
        key1.Should().StartWith($"ct:package:{packageId}:lang:");
        key1.Should().NotContain(":e:",
            "the elevation/admin dimension was removed in the clean public-DTO redesign (P1-007)");
    }
}

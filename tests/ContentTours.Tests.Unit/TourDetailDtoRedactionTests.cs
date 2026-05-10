using ContentTours.Application.Queries.Tour.Common;
using FluentAssertions;

namespace ContentTours.Tests.Unit;

/// <summary>
/// CONTENTTOURS-FOLLOWUP-DTO-REDACTION-001 regression tests.
///
/// <para>
/// <c>GET /tours/{id}</c> and <c>GET /tours/slug/{slug}</c> are
/// <c>AllowAnonymous</c> public read endpoints that return
/// <see cref="TourDetailDto"/>.  Following the same clean-public-DTO principle
/// established by P1-007 (and applied to <c>TourPackageDetailDto</c> and
/// <c>TourPackageSummaryDto</c>), the public DTO must NOT carry owner
/// identity, admin identity, or moderation/audit metadata:
/// </para>
///
/// <list type="bullet">
///   <item><c>CreatedByUserId</c> — owner identity</item>
///   <item><c>ApprovedByUserId</c> / <c>RejectedByUserId</c> — admin identity</item>
///   <item><c>RejectionReason</c> / <c>SuspensionReason</c> — moderation reasons</item>
///   <item><c>SubmittedAt</c> / <c>ApprovedAt</c> / <c>RejectedAt</c> /
///   <c>SuspendedAt</c> / <c>ReinstatedAt</c> — moderation timeline</item>
/// </list>
///
/// <para>
/// These reflection-only tests pin the DTO shape so admin/internal fields cannot
/// silently leak back onto the public response.  Because both
/// <c>GetTourByIdQueryHandler</c> and <c>GetTourBySlugQueryHandler</c> build
/// their response by calling <c>TourDetailDto.From(tour, preferredLanguageId)</c>,
/// pinning the DTO shape automatically pins both projections — the type system
/// guarantees absence at compile time.
/// </para>
/// </summary>
public sealed class TourDetailDtoRedactionTests
{
    // ── 1. Owner identity ─────────────────────────────────────────────────────

    [Fact]
    public void TourDetailDto_DoesNotDeclareCreatedByUserId()
    {
        typeof(TourDetailDto)
            .GetProperty("CreatedByUserId")
            .Should().BeNull(
                "CreatedByUserId must be removed from the public TourDetailDto. " +
                "GET /tours/{id} and GET /tours/slug/{slug} are AllowAnonymous; " +
                "re-introducing this field would leak owner identity to anonymous " +
                "callers (CONTENTTOURS-FOLLOWUP-DTO-REDACTION-001).");
    }

    // ── 2. Admin identity ─────────────────────────────────────────────────────

    [Fact]
    public void TourDetailDto_DoesNotDeclareApprovedByUserId()
    {
        typeof(TourDetailDto)
            .GetProperty("ApprovedByUserId")
            .Should().BeNull(
                "ApprovedByUserId is admin identity and must not appear on the " +
                "public TourDetailDto.");
    }

    [Fact]
    public void TourDetailDto_DoesNotDeclareRejectedByUserId()
    {
        typeof(TourDetailDto)
            .GetProperty("RejectedByUserId")
            .Should().BeNull(
                "RejectedByUserId is admin identity and must not appear on the " +
                "public TourDetailDto.");
    }

    // ── 3. Moderation reasons ─────────────────────────────────────────────────

    [Fact]
    public void TourDetailDto_DoesNotDeclareRejectionReason()
    {
        typeof(TourDetailDto)
            .GetProperty("RejectionReason")
            .Should().BeNull(
                "RejectionReason is moderation metadata and must not appear on the " +
                "public TourDetailDto. Admin/management UI must use a separate " +
                "protected management query/DTO.");
    }

    [Fact]
    public void TourDetailDto_DoesNotDeclareSuspensionReason()
    {
        typeof(TourDetailDto)
            .GetProperty("SuspensionReason")
            .Should().BeNull(
                "SuspensionReason is moderation metadata and must not appear on the " +
                "public TourDetailDto.");
    }

    // ── 4. Moderation timeline (lifecycle audit timestamps) ──────────────────

    [Fact]
    public void TourDetailDto_DoesNotDeclareLifecycleAuditTimestamps()
    {
        var declared = typeof(TourDetailDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        declared.Should().NotContain("SubmittedAt",
            "SubmittedAt leaks moderation submission timing to anonymous callers");
        declared.Should().NotContain("ApprovedAt",
            "ApprovedAt leaks moderation approval timing to anonymous callers");
        declared.Should().NotContain("RejectedAt",
            "RejectedAt leaks moderation rejection timing to anonymous callers");
        declared.Should().NotContain("SuspendedAt",
            "SuspendedAt leaks moderation suspension timing to anonymous callers");
        declared.Should().NotContain("ReinstatedAt",
            "ReinstatedAt leaks moderation reinstatement timing to anonymous callers");
    }

    // ── 5/6. Projection-level guarantees for both endpoints ──────────────────

    [Fact]
    public void GetTourById_PublicProjection_DoesNotExposeOwnerOrModerationFields()
    {
        // GetTourByIdQueryHandler.Handle calls TourDetailDto.From(tour, preferredLanguageId).
        // Because the DTO no longer declares any of the redacted properties, the
        // projection cannot leak them — the type system guarantees absence at
        // compile time.  This test fails if any of those properties is
        // reintroduced on the DTO record.
        AssertPublicShape(typeof(TourDetailDto));
    }

    [Fact]
    public void GetTourBySlug_PublicProjection_DoesNotExposeOwnerOrModerationFields()
    {
        // GetTourBySlugQueryHandler.Handle uses the same TourDetailDto.From(...)
        // factory, so pinning the DTO shape covers this endpoint too.
        AssertPublicShape(typeof(TourDetailDto));
    }

    private static void AssertPublicShape(Type dtoType)
    {
        var declared = dtoType.GetProperties()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Owner / admin identity
        declared.Should().NotContain("CreatedByUserId");
        declared.Should().NotContain("ApprovedByUserId");
        declared.Should().NotContain("RejectedByUserId");

        // Moderation reasons
        declared.Should().NotContain("RejectionReason");
        declared.Should().NotContain("SuspensionReason");

        // Moderation timeline
        declared.Should().NotContain("SubmittedAt");
        declared.Should().NotContain("ApprovedAt");
        declared.Should().NotContain("RejectedAt");
        declared.Should().NotContain("SuspendedAt");
        declared.Should().NotContain("ReinstatedAt");
    }
}

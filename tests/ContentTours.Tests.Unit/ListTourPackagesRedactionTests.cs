using ContentTours.Application.Queries.TourPackage.Common;
using ContentTours.Application.Queries.TourPackage.ListTourPackages;
using ContentTours.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Public package-listing redaction tests (P1-007 follow-up).
///
/// <para>
/// <c>GET /packages</c> is an <c>AllowAnonymous</c> endpoint that returns a list
/// of <see cref="TourPackageSummaryDto"/>.  The same clean-public-DTO principle
/// from <c>GetTourPackageById</c> applies: sensitive / internal fields must NOT
/// be projected onto the public summary.  Specifically the DTO must NOT declare:
/// </para>
///
/// <list type="bullet">
///   <item><c>CreatedByUserId</c></item>
///   <item><c>RowVersion</c></item>
///   <item><c>IsDeleted</c></item>
/// </list>
///
/// <para>
/// These tests pin the DTO shape and the handler projection so admin/internal
/// fields cannot silently leak through the listing again.  If admin/management
/// screens later need any of these fields, a separate **protected** management
/// query / DTO must be introduced.
/// </para>
/// </summary>
public sealed class ListTourPackagesRedactionTests
{
    // ── 1. DTO shape: sensitive fields are not declared on the public summary ─

    [Fact]
    public void TourPackageSummaryDto_DoesNotDeclareCreatedByUserId()
    {
        typeof(TourPackageSummaryDto)
            .GetProperty("CreatedByUserId")
            .Should().BeNull(
                "CreatedByUserId must be removed from the public TourPackageSummaryDto. " +
                "GET /packages is AllowAnonymous; re-introducing this field would leak " +
                "owner identity to anonymous callers (P1-007 follow-up). " +
                "If admin/management screens need it, add a separate protected DTO.");
    }

    [Fact]
    public void TourPackageSummaryDto_DoesNotDeclareRowVersion()
    {
        typeof(TourPackageSummaryDto)
            .GetProperty("RowVersion")
            .Should().BeNull(
                "RowVersion (the optimistic-concurrency token) must not appear on the " +
                "public TourPackageSummaryDto. It is a management/edit concern and must " +
                "be served via a future protected management query.");
    }

    [Fact]
    public void TourPackageSummaryDto_DoesNotDeclareIsDeleted()
    {
        typeof(TourPackageSummaryDto)
            .GetProperty("IsDeleted")
            .Should().BeNull(
                "IsDeleted must not appear on the public TourPackageSummaryDto. " +
                "Soft-delete state is an admin/internal concern; the public listing must " +
                "not expose it (P1-007 follow-up).");
    }

    // ── 2. Handler projection: CreatedByUserId never reaches the response ────

    [Fact]
    public async Task ListTourPackages_DoesNotExposeCreatedByUserId()
    {
        var repo   = Substitute.For<ITourPackageRepository>();
        var logger = Substitute.For<ILogger<ListTourPackagesQueryHandler>>();

        // Repository row record still carries CreatedByUserId — it is the source
        // projection.  The handler must drop it before the public DTO is built.
        var rows = new List<TourPackageSummaryRow>
        {
            new(
                Id:                Guid.NewGuid(),
                CreatedByUserId:   Guid.NewGuid(),
                Name:              "Highlights",
                Description:       null,
                PriceAmount:       150m,
                Currency:          "JOD",
                MaxParticipants:   null,
                ValidFrom:         null,
                ValidTo:           null,
                IncludedTourCount: 2,
                CreatedAt:         DateTime.UtcNow),
        };

        repo.GetPagedSummariesAsync(
                page:             Arg.Any<int>(),
                pageSize:         Arg.Any<int>(),
                providerId:       Arg.Any<Guid?>(),
                minPrice:         Arg.Any<decimal?>(),
                maxPrice:         Arg.Any<decimal?>(),
                currency:         Arg.Any<string?>(),
                includeTourId:    Arg.Any<Guid?>(),
                effectiveDateUtc: Arg.Any<DateTime>(),
                sort:             Arg.Any<TourPackageSortOption>(),
                ct:               Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<TourPackageSummaryRow>)rows, rows.Count));

        var handler = new ListTourPackagesQueryHandler(repo, logger);
        var result  = await handler.Handle(new ListTourPackagesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);

        var dto = result.Value.Items[0];
        dto.GetType().GetProperty("CreatedByUserId").Should().BeNull(
            "the handler must not project CreatedByUserId onto the public summary DTO " +
            "(P1-007 follow-up); the field is intentionally absent from " +
            "TourPackageSummaryDto and is dropped from the row before mapping.");
    }
}

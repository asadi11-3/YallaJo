using System.Linq;
using ContentTours.Application.Commands.Tour.ApproveTour;
using ContentTours.Application.Commands.Tour.CreateTour;
using ContentTours.Application.Commands.Tour.DeleteTour;
using ContentTours.Application.Commands.Tour.RejectTour;
using ContentTours.Application.Commands.Tour.SubmitTour;
using ContentTours.Application.Commands.Tour.SuspendTour;
using ContentTours.Application.Commands.Tour.ToggleTourFeatured;
using ContentTours.Application.Commands.Tour.UpdateTour;
using ContentTours.Application.Commands.Tour.ReinstateTour;
using ContentTours.Application.Commands.ChildrenInfo.Update;
using ContentTours.Application.Interfaces;
using ContentTours.Domain.Entities;
using FluentAssertions;
using YallaJo.SharedKernel.Domain.Entities;

namespace ContentTours.Tests.Unit;

/// <summary>
/// Regression tests for CONTENTTOURS-STD-P1-002 (TourPackage as IAggregateRoot)
/// and CONTENTTOURS-STD-P1-003 (single canonical UnitOfWork).
///
/// Together they ensure:
///  * any future domain event raised on TourPackage will be dispatched, not dropped;
///  * no handler can silently bypass domain-event dispatch by injecting a
///    non-dispatching UnitOfWork — there is now exactly one in the module.
/// </summary>
public sealed class AggregateRootAndUnitOfWorkConsolidationTests
{
    // ── P1-002: TourPackage is an aggregate root ─────────────────────────────

    [Fact]
    public void TourPackage_IsAggregateRoot()
    {
        typeof(IAggregateRoot).IsAssignableFrom(typeof(TourPackage))
            .Should().BeTrue(
                "TourPackage owns its lifecycle, RowVersion, and child collections; " +
                "marking it IAggregateRoot guarantees future domain events are dispatched " +
                "by the shared UnitOfWork.");
    }

    [Fact]
    public void Tour_StillIsAggregateRoot()
    {
        typeof(IAggregateRoot).IsAssignableFrom(typeof(Tour))
            .Should().BeTrue("regression check — Tour must remain an aggregate root.");
    }

    // ── P1-003: only one canonical UnitOfWork interface remains ──────────────

    [Fact]
    public void OnlyOneContentToursUnitOfWorkInterfaceExists()
    {
        var asm  = typeof(IContentToursUnitOfWork).Assembly;
        var uows = asm.GetTypes()
            .Where(t => t.IsInterface
                     && t.Name.StartsWith("IContentTours", System.StringComparison.Ordinal)
                     && t.Name.EndsWith("UnitOfWork", System.StringComparison.Ordinal))
            .ToList();

        uows.Should().ContainSingle(
            "the dual UnitOfWork split has been consolidated; " +
            "IContentToursEventUnitOfWork must no longer exist as a separate type.");
        uows[0].Should().Be(typeof(IContentToursUnitOfWork));
    }

    // ── P1-003: every Tour-aggregate command handler now binds to the canonical
    // event-dispatching UnitOfWork (no handler accidentally left on a plain
    // non-dispatching path that would drop domain events).
    [Theory]
    [InlineData(typeof(CreateTourCommandHandler))]
    [InlineData(typeof(UpdateTourCommandHandler))]
    [InlineData(typeof(DeleteTourCommandHandler))]
    [InlineData(typeof(SubmitTourCommandHandler))]
    [InlineData(typeof(ApproveTourCommandHandler))]
    [InlineData(typeof(RejectTourCommandHandler))]
    [InlineData(typeof(SuspendTourCommandHandler))]
    [InlineData(typeof(ReinstateTourCommandHandler))]
    [InlineData(typeof(ToggleTourFeaturedCommandHandler))]
    [InlineData(typeof(UpdateTourChildrenInfoCommandHandler))]
    public void TourAggregateHandlers_DependOnCanonicalUnitOfWork(System.Type handlerType)
    {
        var ctor = handlerType.GetConstructors().Single();

        ctor.GetParameters()
            .Any(p => p.ParameterType == typeof(IContentToursUnitOfWork))
            .Should().BeTrue(
                $"{handlerType.Name} must inject IContentToursUnitOfWork so that " +
                "Tour aggregate domain events are dispatched on save.");
    }
}

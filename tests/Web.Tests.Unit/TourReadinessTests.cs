using FluentAssertions;
using YallaJo.Web.Areas.Provider.Facades;
using YallaJo.Web.Areas.Provider.Models.TourImages;
using YallaJo.Web.Areas.Provider.Models.TourPricing;
using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Areas.Provider.Models.TourSchedules;

namespace Web.Tests.Unit;

/// <summary>
/// Unit tests for <see cref="ProviderToursFacade.ComputeReadiness"/>, the persisted-data
/// projection that mirrors the backend pre-submit gate (SubmitTourCommandHandler). These
/// lock in the rule that wizard check marks and the Review &amp; Submit panel reflect SAVED
/// data, not which step the user visited.
/// </summary>
public sealed class TourReadinessTests
{
    private static readonly Guid TourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PlaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string ValidDescription =
        "This is a sufficiently long tour description that comfortably exceeds the one hundred character minimum length rule.";

    private static TourPricingTierResponse Tier(string participantType, bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        TourId = TourId,
        Name = participantType,
        ParticipantType = participantType,
        Price = 10m,
        Currency = "USD",
        IsActive = isActive,
    };

    private static TourScheduleResponse Schedule(bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        TourId = TourId,
        DayOfWeek = 1,
        StartTime = "09:00",
        IsActive = isActive,
    };

    private static AttachmentItemResponse Image() => new()
    {
        Id = Guid.NewGuid(),
        EntityType = "Tour",
        EntityId = TourId,
        Type = "Image",
        Url = "/uploads/tours/x.png",
    };

    private static TourReadinessVm Compute(
        string? description = ValidDescription,
        Guid? placeId = null,
        decimal? lat = 31.9m,
        decimal? lng = 35.9m,
        IReadOnlyList<TourPricingTierResponse>? pricing = null,
        IReadOnlyList<TourScheduleResponse>? schedules = null,
        IReadOnlyList<AttachmentItemResponse>? images = null,
        bool degraded = false)
        => ProviderToursFacade.ComputeReadiness(
            TourId,
            description,
            placeId ?? PlaceId,
            lat,
            lng,
            pricing ?? [Tier("Adult")],
            schedules ?? [Schedule()],
            images ?? [Image()],
            degraded);

    [Fact]
    public void AllRequirementsMet_IsComplete_WithNoMissingItems()
    {
        var vm = Compute();

        vm.BasicsComplete.Should().BeTrue();
        vm.PricingComplete.Should().BeTrue();
        vm.ScheduleComplete.Should().BeTrue();
        vm.ImagesComplete.Should().BeTrue();
        vm.AllComplete.Should().BeTrue();
        vm.IsDegraded.Should().BeFalse();
        vm.MissingRequirementKeys.Should().BeEmpty();
        vm.StepCompletion[5].Should().BeTrue();
    }

    [Fact]
    public void MissingPlace_MarksBasicsIncomplete_AndListsPlace()
    {
        var vm = Compute(placeId: Guid.Empty);

        vm.BasicsComplete.Should().BeFalse();
        vm.AllComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingPlace");
        vm.StepCompletion[1].Should().BeFalse();
    }

    [Fact]
    public void MissingMeetingPoint_MarksBasicsIncomplete()
    {
        var vm = Compute(lat: null, lng: null);

        vm.BasicsComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingMeetingPoint");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("too short")]
    public void MissingOrShortDescription_MarksBasicsIncomplete(string? description)
    {
        var vm = Compute(description: description);

        vm.BasicsComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingDescription");
    }

    [Fact]
    public void DescriptionExactly100Chars_CountsAsComplete()
    {
        var exactly100 = new string('a', 100);

        var vm = Compute(description: exactly100);

        vm.BasicsComplete.Should().BeTrue();
        vm.MissingRequirementKeys.Should().NotContain("Provider.TourReadiness.MissingDescription");
    }

    [Fact]
    public void Description99Chars_IsTooShort()
    {
        var ninetyNine = new string('a', 99);

        var vm = Compute(description: ninetyNine);

        vm.BasicsComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingDescription");
    }

    [Fact]
    public void NoPricingTiers_ListsMissingPricing_NotMissingAdult()
    {
        var vm = Compute(pricing: []);

        vm.PricingComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingPricing");
        vm.MissingRequirementKeys.Should().NotContain("Provider.TourReadiness.MissingAdultPricing");
    }

    [Fact]
    public void OnlyNonAdultActiveTier_ListsMissingAdultPricing()
    {
        var vm = Compute(pricing: [Tier("Child")]);

        vm.PricingComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingAdultPricing");
        vm.MissingRequirementKeys.Should().NotContain("Provider.TourReadiness.MissingPricing");
    }

    [Fact]
    public void InactiveAdultTier_DoesNotSatisfyPricing()
    {
        var vm = Compute(pricing: [Tier("Adult", isActive: false)]);

        vm.PricingComplete.Should().BeFalse();
        // No active tier at all → treated as "no pricing".
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingPricing");
    }

    [Fact]
    public void AdultParticipantType_IsCaseInsensitive()
    {
        var vm = Compute(pricing: [Tier("adult")]);

        vm.PricingComplete.Should().BeTrue();
    }

    [Fact]
    public void NoActiveSchedule_ListsMissingSchedule()
    {
        var vm = Compute(schedules: [Schedule(isActive: false)]);

        vm.ScheduleComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingSchedule");
    }

    [Fact]
    public void NoImages_ListsMissingImages()
    {
        var vm = Compute(images: []);

        vm.ImagesComplete.Should().BeFalse();
        vm.MissingRequirementKeys.Should().Contain("Provider.TourReadiness.MissingImages");
        vm.StepCompletion[4].Should().BeFalse();
    }

    [Fact]
    public void DegradedFlag_IsPreserved_WhenSubResourceLookupFailed()
    {
        // null collections simulate a failed sub-resource lookup; facade flags degraded.
        // Call ComputeReadiness directly so the null collections are NOT coalesced away
        // by the Compute(...) helper's valid defaults.
        var vm = ProviderToursFacade.ComputeReadiness(
            TourId,
            ValidDescription,
            PlaceId,
            31.9m,
            35.9m,
            pricing: null,
            schedules: null,
            images: null,
            degraded: true);

        vm.IsDegraded.Should().BeTrue();
        vm.PricingComplete.Should().BeFalse();
        vm.ScheduleComplete.Should().BeFalse();
        vm.ImagesComplete.Should().BeFalse();
    }

    [Fact]
    public void MissingRequirements_AreOrdered_BasicsThenPricingThenScheduleThenImages()
    {
        var vm = Compute(
            placeId: Guid.Empty,
            lat: null,
            lng: null,
            description: null,
            pricing: [],
            schedules: [Schedule(isActive: false)],
            images: []);

        vm.MissingRequirementKeys.Should().ContainInOrder(
            "Provider.TourReadiness.MissingPlace",
            "Provider.TourReadiness.MissingMeetingPoint",
            "Provider.TourReadiness.MissingDescription",
            "Provider.TourReadiness.MissingPricing",
            "Provider.TourReadiness.MissingSchedule",
            "Provider.TourReadiness.MissingImages");
    }
}

using Booking.Application.Commands.JoinRequest.CreateJoinRequest;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Booking.Tests.Unit.Commands.JoinRequest;

public sealed class CreateJoinRequestCommandValidatorTests
{
    private readonly CreateJoinRequestCommandValidator _sut = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _sut.TestValidate(new CreateJoinRequestCommand(Guid.NewGuid(), 2, "hi"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_booking_id_fails()
    {
        var result = _sut.TestValidate(new CreateJoinRequestCommand(Guid.Empty, 1, null));
        result.ShouldHaveValidationErrorFor(x => x.BookingId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Invalid_participant_count_fails(int count)
    {
        var result = _sut.TestValidate(new CreateJoinRequestCommand(Guid.NewGuid(), count, null));
        result.ShouldHaveValidationErrorFor(x => x.ParticipantCount);
    }

    [Fact]
    public void Message_exceeding_max_length_fails()
    {
        var tooLong = new string('a', CreateJoinRequestCommandValidator.MaxMessageLength + 1);
        var result = _sut.TestValidate(new CreateJoinRequestCommand(Guid.NewGuid(), 1, tooLong));
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }
}

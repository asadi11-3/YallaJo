using System.Linq.Expressions;
using ContentPlaces.Application.Commands.BusinessStaff.AddBusinessStaff;
using ContentPlaces.Application.Interfaces;
using ContentPlaces.Domain.Entities;
using ContentPlaces.Domain.Enums;
using ContentPlaces.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using NSubstitute;
using YallaJo.SharedKernel.Application.Abstractions.Context;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace ContentPlaces.Tests.Unit;

public sealed class AddBusinessStaffCommandHandlerTests
{
    private static (
        AddBusinessStaffCommandHandler Handler,
        IBusinessStaffRepository StaffRepo,
        IBusinessRepository BusinessRepo,
        IContentPlacesUnitOfWork Uow,
        ICurrentUser CurrentUser) BuildSubject()
    {
        var staffRepo = Substitute.For<IBusinessStaffRepository>();
        var businessRepo = Substitute.For<IBusinessRepository>();
        var uow = Substitute.For<IContentPlacesUnitOfWork>();
        var outbox = Substitute.For<IContentPlacesOutboxWriter>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(Guid.NewGuid()); // default; tests override below
        var cache = Substitute.For<HybridCache>();
        var logger = Substitute.For<ILogger<AddBusinessStaffCommandHandler>>();

        var handler = new AddBusinessStaffCommandHandler(
            staffRepo, businessRepo, uow, outbox, cache, currentUser, logger);

        return (handler, staffRepo, businessRepo, uow, currentUser);
    }

    [Fact]
    public async Task ReturnsNotFoundWhenBusinessMissing()
    {
        var (handler, _, businessRepo, _, _) = BuildSubject();
        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Business?)null);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(Guid.NewGuid(), Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "NotFound.Business.NotFound");
    }

    [Fact]
    public async Task ReturnsForbiddenWhenActingUserIsNotOwner()
    {
        var (handler, _, businessRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        currentUser.UserId.Returns(callerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "Business.Forbidden");
    }

    [Fact]
    public async Task ReturnsConflictWhenStaffAlreadyActive()
    {
        var (handler, staffRepo, businessRepo, _, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        currentUser.UserId.Returns(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessStaff, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Staff),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(x => x.Code == "BusinessStaff.Duplicate.Conflict");
    }

    [Fact]
    public async Task SucceedsWhenActingUserIsOwner()
    {
        var (handler, staffRepo, businessRepo, uow, currentUser) = BuildSubject();
        var ownerId = Guid.NewGuid();
        var business = TestBusinessFactory.CreateBusiness(ownerId);
        currentUser.UserId.Returns(ownerId);

        businessRepo
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(business);
        staffRepo
            .AnyAsync(
                Arg.Any<Expression<Func<BusinessStaff, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(
            new AddBusinessStaffCommand(business.Id, Guid.NewGuid(), BusinessStaffRole.Manager),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(Outcome.Created);
        await staffRepo.Received(1).AddAsync(Arg.Any<BusinessStaff>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

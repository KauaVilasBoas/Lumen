using AegisIdentity.CommandHandlers.UserProfiles.RemoveUserProfile;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace AegisIdentity.UnitTests.Application.UserProfiles;

public sealed class RemoveUserProfileCommandHandlerTests
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;
    private readonly RemoveUserProfileCommandHandler _sut;

    public RemoveUserProfileCommandHandlerTests()
    {
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _publisher = Substitute.For<IPublisher>();

        _sut = new RemoveUserProfileCommandHandler(_userProfileRepository, _publisher);
    }

    [Fact]
    public async Task Handle_WhenAssignmentNotFound_ThrowsNotFoundException()
    {
        _userProfileRepository.FindActiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var act = async () => await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAssignmentFound_SoftDeletesAndPublishesPermissionsChanged()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var userProfile = UserProfile.Create(userId, profileId);

        _userProfileRepository.FindActiveAsync(userId, profileId, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(userId, profileId),
            CancellationToken.None);

        await _userProfileRepository.Received(1).UpdateAsync(
            Arg.Is<UserProfile>(up => up.IsDeleted),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NeverPhysicallyDeletesRow()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var userProfile = UserProfile.Create(userId, profileId);

        _userProfileRepository.FindActiveAsync(userId, profileId, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(userId, profileId),
            CancellationToken.None);

        userProfile.IsDeleted.Should().BeTrue("soft-delete must be used, never physical delete");
        userProfile.DeletedAt.Should().NotBeNull();
    }
}

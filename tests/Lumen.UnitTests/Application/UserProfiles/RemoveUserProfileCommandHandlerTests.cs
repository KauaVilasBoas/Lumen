using Lumen.CommandHandlers.UserProfiles.RemoveUserProfile;
using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Lumen.UnitTests.Application.UserProfiles;

public sealed class RemoveUserProfileCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;
    private readonly RemoveUserProfileCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();

    public RemoveUserProfileCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _profileRepository = Substitute.For<IProfileRepository>();
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _publisher = Substitute.For<IPublisher>();

        _sut = new RemoveUserProfileCommandHandler(
            _userRepository,
            _profileRepository,
            _userProfileRepository,
            _publisher);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = async () => await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(UserId, ProfileId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(User.Create("u@test.com", "user", "hash"));
        _profileRepository.FindByIdAsync(ProfileId, Arg.Any<CancellationToken>()).ReturnsNull();

        var act = async () => await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(UserId, ProfileId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAssignmentNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(User.Create("u@test.com", "user", "hash"));
        _profileRepository.FindByIdAsync(ProfileId, Arg.Any<CancellationToken>())
            .Returns(Profile.Create("test-profile", "test"));
        _userProfileRepository.FindActiveAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .ReturnsNull();

        var act = async () => await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(UserId, ProfileId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAssignmentFound_SoftDeletesAndPublishesPermissionsChanged()
    {
        var userProfile = UserProfile.Create(UserId, ProfileId);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(User.Create("u@test.com", "user", "hash"));
        _profileRepository.FindByIdAsync(ProfileId, Arg.Any<CancellationToken>())
            .Returns(Profile.Create("test-profile", "test"));
        _userProfileRepository.FindActiveAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(UserId, ProfileId),
            CancellationToken.None);

        await _userProfileRepository.Received(1).UpdateAsync(
            Arg.Is<UserProfile>(up => up.IsDeleted),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == UserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NeverPhysicallyDeletesRow()
    {
        var userProfile = UserProfile.Create(UserId, ProfileId);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(User.Create("u@test.com", "user", "hash"));
        _profileRepository.FindByIdAsync(ProfileId, Arg.Any<CancellationToken>())
            .Returns(Profile.Create("test-profile", "test"));
        _userProfileRepository.FindActiveAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(UserId, ProfileId),
            CancellationToken.None);

        userProfile.IsDeleted.Should().BeTrue("soft-delete must be used, never physical delete");
        userProfile.DeletedAt.Should().NotBeNull();
    }
}

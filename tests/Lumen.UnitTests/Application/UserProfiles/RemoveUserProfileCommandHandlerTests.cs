using Lumen.CommandHandlers.UserProfiles.RemoveUserProfile;
using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace Lumen.UnitTests.Application.UserProfiles;

public sealed class RemoveUserProfileCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly RemoveUserProfileCommandHandler _sut;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProfileId = Guid.NewGuid();

    public RemoveUserProfileCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _profileRepository = Substitute.For<IProfileRepository>();
        _userProfileRepository = Substitute.For<IUserProfileRepository>();

        _sut = new RemoveUserProfileCommandHandler(
            _userRepository,
            _profileRepository,
            _userProfileRepository);
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
    public async Task Handle_WhenAssignmentFound_SoftDeletesAndRaisesDomainEvents()
    {
        var user = User.Create("u@test.com", "user", "hash");
        var profile = Profile.Create("test-profile", "test");
        var userProfile = UserProfile.Create(UserId, ProfileId);

        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _profileRepository.FindByIdAsync(ProfileId, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository.FindActiveAsync(UserId, ProfileId, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        await _sut.Handle(
            new RemoveUserProfileCommandHandler.Command(UserId, ProfileId),
            CancellationToken.None);

        await _userProfileRepository.Received(1).UpdateAsync(
            Arg.Is<UserProfile>(up => up.IsDeleted),
            Arg.Any<CancellationToken>());

        user.DomainEvents.Should().ContainSingle(e => e is UserPermissionsChanged
            && ((UserPermissionsChanged)e).UserId == user.Id);

        user.DomainEvents.Should().ContainSingle(e => e is UserProfileRemoved
            && ((UserProfileRemoved)e).UserId == user.Id
            && ((UserProfileRemoved)e).ProfileId == profile.Id
            && ((UserProfileRemoved)e).ProfileName == profile.Name);
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

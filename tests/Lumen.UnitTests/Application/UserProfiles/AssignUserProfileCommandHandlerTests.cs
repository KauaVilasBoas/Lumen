using AegisIdentity.CommandHandlers.UserProfiles.AssignUserProfile;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using DomainProfile = AegisIdentity.Domain.Authorization.Profile;

namespace AegisIdentity.UnitTests.Application.UserProfiles;

public sealed class AssignUserProfileCommandHandlerTests
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;
    private readonly AssignUserProfileCommandHandler _sut;

    public AssignUserProfileCommandHandlerTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _profileRepository = Substitute.For<IProfileRepository>();
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _publisher = Substitute.For<IPublisher>();

        _sut = new AssignUserProfileCommandHandler(
            _userRepository,
            _profileRepository,
            _userProfileRepository,
            _publisher);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = async () => await _sut.Handle(
            new AssignUserProfileCommandHandler.Command(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        var user = User.Create("test@test.com", "testuser", "hash");
        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _profileRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DomainProfile?)null);

        var act = async () => await _sut.Handle(
            new AssignUserProfileCommandHandler.Command(user.Id, Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAssignmentAlreadyExists_IsIdempotentAndDoesNotInsert()
    {
        var user = User.Create("test@test.com", "testuser", "hash");
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var existing = UserProfile.Create(user.Id, profile.Id);

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository.FindActiveAsync(user.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _sut.Handle(
            new AssignUserProfileCommandHandler.Command(user.Id, profile.Id),
            CancellationToken.None);

        await _userProfileRepository.DidNotReceive().InsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNewAssignment_InsertsAndPublishesPermissionsChanged()
    {
        var user = User.Create("test@test.com", "testuser", "hash");
        var profile = DomainProfile.Create("Editors", "Editors profile");

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository.FindActiveAsync(user.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        await _sut.Handle(
            new AssignUserProfileCommandHandler.Command(user.Id, profile.Id),
            CancellationToken.None);

        await _userProfileRepository.Received(1).InsertAsync(
            Arg.Is<UserProfile>(up => up.UserId == user.Id && up.ProfileId == profile.Id),
            Arg.Any<CancellationToken>());

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == user.Id),
            Arg.Any<CancellationToken>());
    }
}

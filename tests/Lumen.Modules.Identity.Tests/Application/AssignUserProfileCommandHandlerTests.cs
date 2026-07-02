using FluentAssertions;
using Lumen.Authorization.Application.UserProfiles.Assign;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Modularity;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class AssignUserProfileCommandHandlerTests
{
    private readonly IUserDirectory _userDirectory = Substitute.For<IUserDirectory>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IUserProfileRepository _userProfileRepository = Substitute.For<IUserProfileRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private AssignUserProfileCommandHandler CreateHandler()
        => new(_userDirectory, _profileRepository, _userProfileRepository, _eventBus);

    [Fact]
    public async Task Handle_ValidAssignment_InsertsAndPublishesBothEvents()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("Admin", "Administrator profile");

        _userDirectory.GetDisplayNameAsync(userId, Arg.Any<CancellationToken>()).Returns("user");
        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository.FindActiveAsync(userId, profileId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);

        var handler = CreateHandler();
        await handler.Handle(new AssignUserProfileCommand(userId, profileId), CancellationToken.None);

        await _userProfileRepository.Received(1).InsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserPermissionsChangedEvent>(e => e.UserId == userId),
            Arg.Any<CancellationToken>());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserProfileAssignedEvent>(e =>
                e.UserId == userId &&
                e.ProfileId == profileId &&
                e.ProfileName == "Admin"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyAssigned_DoesNotInsertOrPublish()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("Admin", "desc");
        var existingAssignment = UserProfile.Create(userId, profileId);

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository.FindActiveAsync(userId, profileId, Arg.Any<CancellationToken>()).Returns(existingAssignment);

        var handler = CreateHandler();
        await handler.Handle(new AssignUserProfileCommand(userId, profileId), CancellationToken.None);

        await _userProfileRepository.DidNotReceive().InsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>());
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Profile?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new AssignUserProfileCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationNotFoundException>();
    }
}

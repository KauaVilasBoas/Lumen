using FluentAssertions;
using Lumen.Modularity;
using Lumen.Modules.Identity.Application.Profiles.Delete;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class DeleteProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IUserProfileRepository _userProfileRepository = Substitute.For<IUserProfileRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private DeleteProfileCommandHandler CreateHandler()
        => new(_profileRepository, _userProfileRepository, _eventBus);

    [Fact]
    public async Task Handle_ValidProfile_SoftDeletesAndPublishesPermissionsChangedForAffectedUsers()
    {
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("Managers", "Manages resources");
        var affectedUserId = Guid.NewGuid();

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _profileRepository
            .GetUserIdsByProfileIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns([affectedUserId]);
        _profileRepository
            .GetActivePermissionProfilesByProfileIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns([]);
        _userProfileRepository
            .ListByProfileIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new DeleteProfileCommand(profileId), CancellationToken.None);

        await _profileRepository.Received(1).DeleteWithCascadeAsync(
            Arg.Any<Profile>(),
            Arg.Any<IReadOnlyList<PermissionProfile>>(),
            Arg.Any<IReadOnlyList<UserProfile>>(),
            Arg.Any<CancellationToken>());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserPermissionsChangedEvent>(e => e.UserId == affectedUserId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Profile?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new DeleteProfileCommand(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SystemProfile_ThrowsForbiddenException()
    {
        var profileId = Guid.NewGuid();
        var systemProfile = Profile.Create("Administrator", "System managed", isSystem: true);

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(systemProfile);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new DeleteProfileCommand(profileId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_ProfileWithNoAffectedUsers_DeletesWithoutPublishingPermissionsEvent()
    {
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("EmptyProfile", "No users assigned");

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _profileRepository
            .GetUserIdsByProfileIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns([]);
        _profileRepository
            .GetActivePermissionProfilesByProfileIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns([]);
        _userProfileRepository
            .ListByProfileIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new DeleteProfileCommand(profileId), CancellationToken.None);

        await _eventBus.DidNotReceive().PublishAsync(
            Arg.Any<UserPermissionsChangedEvent>(),
            Arg.Any<CancellationToken>());
    }
}

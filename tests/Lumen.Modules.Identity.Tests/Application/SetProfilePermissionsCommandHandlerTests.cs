using FluentAssertions;
using Lumen.Authorization.Application.Profiles.SetPermissions;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Modularity;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class SetProfilePermissionsCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IPermissionRepository _permissionRepository = Substitute.For<IPermissionRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private SetProfilePermissionsCommandHandler CreateHandler()
        => new(_profileRepository, _permissionRepository, _eventBus);

    [Fact]
    public async Task Handle_SystemProfile_ThrowsForbiddenException()
    {
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("Admin", "desc", isSystem: true);

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new SetProfilePermissionsCommand(profileId, []),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationForbiddenException>();
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Profile?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new SetProfilePermissionsCommand(Guid.NewGuid(), []),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationNotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidUpdate_PublishesProfilePermissionsSetEvent()
    {
        var profileId = Guid.NewGuid();
        var permId = Guid.NewGuid();
        var profile = Profile.Create("Readers", "desc");
        var permission = Permission.Create("Resources", "Read");

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _permissionRepository.FindByIdAsync(permId, Arg.Any<CancellationToken>()).Returns(permission);
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profileId, Arg.Any<CancellationToken>()).Returns([]);
        _profileRepository.GetUserIdsByProfileIdAsync(profileId, Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();
        await handler.Handle(
            new SetProfilePermissionsCommand(profileId, [permId], "actor"),
            CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<ProfilePermissionsSetEvent>(e =>
                e.ProfileId == profileId &&
                e.ProfileName == "Readers" &&
                e.ActorUsername == "actor"),
            Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Authorization.Application.UserProfiles.Remove;
using Lumen.Authorization.Contracts;
using Lumen.Authorization.Contracts.Events;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using Lumen.Modularity;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class RemoveUserProfileCommandHandlerTests
{
    private readonly IUserDirectory _userDirectory = Substitute.For<IUserDirectory>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IUserProfileRepository _userProfileRepository = Substitute.For<IUserProfileRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private RemoveUserProfileCommandHandler CreateHandler()
        => new(_userDirectory, _profileRepository, _userProfileRepository, _eventBus);

    [Fact]
    public async Task Handle_ValidGlobalRemoval_SoftDeletesAndPublishesBothEvents()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("Admin", "Administrator profile");
        var userProfile = UserProfile.Create(userId, profileId);

        _userDirectory.GetDisplayNameAsync(userId, Arg.Any<CancellationToken>()).Returns("user");
        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository
            .FindActiveAsync(userId, profileId, null, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        var handler = CreateHandler();
        await handler.Handle(new RemoveUserProfileCommand(userId, profileId), CancellationToken.None);

        await _userProfileRepository.Received(1).UpdateAsync(
            Arg.Is<UserProfile>(up => up.IsDeleted),
            Arg.Any<CancellationToken>());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserPermissionsChangedEvent>(e => e.UserId == userId && e.ScopeId == null),
            Arg.Any<CancellationToken>());

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserProfileRemovedEvent>(e =>
                e.UserId == userId &&
                e.ProfileId == profileId &&
                e.ProfileName == "Admin"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidScopedRemoval_PublishesScopedInvalidationEvent()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();
        var profile = Profile.Create("Admin", "Administrator profile");
        var userProfile = UserProfile.Create(userId, profileId, scopeId);

        _userDirectory.GetDisplayNameAsync(userId, Arg.Any<CancellationToken>()).Returns("user");
        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository
            .FindActiveAsync(userId, profileId, scopeId, Arg.Any<CancellationToken>())
            .Returns(userProfile);

        var handler = CreateHandler();
        await handler.Handle(new RemoveUserProfileCommand(userId, profileId, scopeId), CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserPermissionsChangedEvent>(e => e.UserId == userId && e.ScopeId == scopeId),
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
            new RemoveUserProfileCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationNotFoundException>();
    }

    [Fact]
    public async Task Handle_AssignmentNotFound_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("Admin", "desc");

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _userProfileRepository
            .FindActiveAsync(userId, profileId, null, Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RemoveUserProfileCommand(userId, profileId),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationNotFoundException>();
    }

    [Fact]
    public void Validator_EmptyUserId_ProducesError()
    {
        var validator = new RemoveUserProfileCommandHandler.Validator();
        var result = validator.TestValidate(new RemoveUserProfileCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Validator_EmptyProfileId_ProducesError()
    {
        var validator = new RemoveUserProfileCommandHandler.Validator();
        var result = validator.TestValidate(new RemoveUserProfileCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ProfileId);
    }

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        var validator = new RemoveUserProfileCommandHandler.Validator();
        var result = validator.TestValidate(
            new RemoveUserProfileCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

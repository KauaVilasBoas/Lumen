using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Authorization.Application.Profiles.Update;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    private UpdateProfileCommandHandler CreateHandler()
        => new(_profileRepository);

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesProfile()
    {
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("OldName", "Old description");

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _profileRepository
            .ActiveNameExistsAsync("NewName", profileId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        await handler.Handle(
            new UpdateProfileCommand(profileId, "NewName", "New description"),
            CancellationToken.None);

        await _profileRepository.Received(1).UpdateAsync(
            Arg.Is<Profile>(p => p.Name == "NewName" && p.Description == "New description"),
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
            new UpdateProfileCommand(Guid.NewGuid(), "Name", "Desc"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationNotFoundException>();
    }

    [Fact]
    public async Task Handle_SystemProfileRename_ThrowsForbiddenException()
    {
        var profileId = Guid.NewGuid();
        var systemProfile = Profile.Create("Administrator", "System managed", isSystem: true);

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(systemProfile);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new UpdateProfileCommand(profileId, "NewName", "Desc"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationForbiddenException>();
    }

    [Fact]
    public async Task Handle_SystemProfileDescriptionUpdate_Succeeds()
    {
        var profileId = Guid.NewGuid();
        var systemProfile = Profile.Create("Administrator", "Old description", isSystem: true);

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(systemProfile);
        _profileRepository
            .ActiveNameExistsAsync("Administrator", profileId, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        await handler.Handle(
            new UpdateProfileCommand(profileId, "Administrator", "New description"),
            CancellationToken.None);

        await _profileRepository.Received(1).UpdateAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflictException()
    {
        var profileId = Guid.NewGuid();
        var profile = Profile.Create("OldName", "Old description");

        _profileRepository.FindByIdAsync(profileId, Arg.Any<CancellationToken>()).Returns(profile);
        _profileRepository
            .ActiveNameExistsAsync("TakenName", profileId, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new UpdateProfileCommand(profileId, "TakenName", "Desc"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationConflictException>();
    }

    [Fact]
    public void Validator_EmptyId_ProducesError()
    {
        var validator = new UpdateProfileCommandHandler.Validator();
        var result = validator.TestValidate(new UpdateProfileCommand(Guid.Empty, "Name", "Desc"));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validator_EmptyName_ProducesError()
    {
        var validator = new UpdateProfileCommandHandler.Validator();
        var result = validator.TestValidate(new UpdateProfileCommand(Guid.NewGuid(), "", "Desc"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        var validator = new UpdateProfileCommandHandler.Validator();
        var result = validator.TestValidate(
            new UpdateProfileCommand(Guid.NewGuid(), "Managers", "Manages resources"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

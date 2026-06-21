using Lumen.CommandHandlers.Profiles.UpdateProfile;
using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using NSubstitute;
using DomainProfile = Lumen.Domain.Authorization.Profile;

namespace Lumen.UnitTests.Application.Profiles;

public sealed class UpdateProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository;
    private readonly UpdateProfileCommandHandler _sut;

    public UpdateProfileCommandHandlerTests()
    {
        _profileRepository = Substitute.For<IProfileRepository>();
        _sut = new UpdateProfileCommandHandler(_profileRepository);
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DomainProfile?)null);

        var act = async () => await _sut.Handle(
            new UpdateProfileCommandHandler.Command(Guid.NewGuid(), "NewName", "Desc"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSystemProfileRename_ThrowsForbiddenException()
    {
        var systemProfile = DomainProfile.Create("Administrator", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        var act = async () => await _sut.Handle(
            new UpdateProfileCommandHandler.Command(systemProfile.Id, "NewName", "Updated description"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*cannot be renamed*");
    }

    [Fact]
    public async Task Handle_WhenSystemProfileSameName_AllowsDescriptionUpdate()
    {
        var systemProfile = DomainProfile.Create("Administrator", "Old description", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);
        _profileRepository
            .ActiveNameExistsAsync("Administrator", systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.Handle(
            new UpdateProfileCommandHandler.Command(systemProfile.Id, "Administrator", "New description"),
            CancellationToken.None);

        await _profileRepository.Received(1).UpdateAsync(
            Arg.Is<DomainProfile>(p => p.Description == "New description"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSystemProfileRename_DoesNotPersistChanges()
    {
        var systemProfile = DomainProfile.Create("Administrator", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        try
        {
            await _sut.Handle(
                new UpdateProfileCommandHandler.Command(systemProfile.Id, "Renamed", "Desc"),
                CancellationToken.None);
        }
        catch (ForbiddenException) { }

        await _profileRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<DomainProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNonSystemProfile_AllowsRename()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository
            .ActiveNameExistsAsync("Writers", profile.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        await _sut.Handle(
            new UpdateProfileCommandHandler.Command(profile.Id, "Writers", "Updated description"),
            CancellationToken.None);

        await _profileRepository.Received(1).UpdateAsync(
            Arg.Is<DomainProfile>(p => p.Name == "Writers"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyInUse_ThrowsConflictException()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository
            .ActiveNameExistsAsync("Writers", profile.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var act = async () => await _sut.Handle(
            new UpdateProfileCommandHandler.Command(profile.Id, "Writers", "Desc"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}

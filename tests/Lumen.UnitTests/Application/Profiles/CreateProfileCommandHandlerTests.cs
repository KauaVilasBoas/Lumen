using Lumen.CommandHandlers.Profiles.CreateProfile;
using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using NSubstitute;
using DomainProfile = Lumen.Domain.Authorization.Profile;

namespace Lumen.UnitTests.Application.Profiles;

public sealed class CreateProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository;
    private readonly CreateProfileCommandHandler _sut;

    public CreateProfileCommandHandlerTests()
    {
        _profileRepository = Substitute.For<IProfileRepository>();
        _sut = new CreateProfileCommandHandler(_profileRepository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_InsertsProfileAndReturnsResult()
    {
        _profileRepository
            .ActiveNameExistsAsync("Editors", null, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new CreateProfileCommandHandler.Command("Editors", "Content editors profile");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Name.Should().Be("Editors");
        result.Description.Should().Be("Content editors profile");
        result.Id.Should().NotBe(Guid.Empty);

        await _profileRepository.Received(1).InsertAsync(Arg.Is<DomainProfile>(
            p => p.Name == "Editors"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ThrowsConflictException()
    {
        _profileRepository
            .ActiveNameExistsAsync("Editors", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var cmd = new CreateProfileCommandHandler.Command("Editors", "Another editors profile");

        var act = async () => await _sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _profileRepository.DidNotReceive().InsertAsync(Arg.Any<DomainProfile>(), Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Authorization.Application.Profiles.Create;
using Lumen.Authorization.Domain;
using Lumen.Authorization.Exceptions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class CreateProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    private CreateProfileCommandHandler CreateHandler()
        => new(_profileRepository);

    [Fact]
    public async Task Handle_UniqueName_CreatesProfileAndReturnsResult()
    {
        _profileRepository
            .ActiveNameExistsAsync("Managers", null, Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new CreateProfileCommand("Managers", "Manages resources"),
            CancellationToken.None);

        result.Name.Should().Be("Managers");
        result.Description.Should().Be("Manages resources");
        result.Id.Should().NotBeEmpty();

        await _profileRepository.Received(1).InsertAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflictException()
    {
        _profileRepository
            .ActiveNameExistsAsync("Managers", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new CreateProfileCommand("Managers", "Desc"),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationConflictException>();
        await _profileRepository.DidNotReceive().InsertAsync(Arg.Any<Profile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyName_ProducesError()
    {
        var validator = new CreateProfileCommandHandler.Validator();
        var result = validator.TestValidate(new CreateProfileCommand("", "Desc"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_NameTooLong_ProducesError()
    {
        var longName = new string('a', 129);
        var validator = new CreateProfileCommandHandler.Validator();
        var result = validator.TestValidate(new CreateProfileCommand(longName, "Desc"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validator_EmptyDescription_ProducesError()
    {
        var validator = new CreateProfileCommandHandler.Validator();
        var result = validator.TestValidate(new CreateProfileCommand("Name", ""));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        var validator = new CreateProfileCommandHandler.Validator();
        var result = validator.TestValidate(new CreateProfileCommand("Managers", "Manages resources"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

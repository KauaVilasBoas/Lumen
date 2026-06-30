using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.ConfirmEmail;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class ConfirmEmailCommandHandlerTests
{
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private ConfirmEmailCommandHandler CreateHandler()
        => new(_tokenRepository, _userRepository);

    [Fact]
    public async Task Handle_ValidToken_ActivatesUserAndMarksTokenUsed()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        var rawToken = "valid_confirmation_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var confirmationToken = EmailConfirmationToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(24));

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(confirmationToken);
        _userRepository.FindByIdAsync(confirmationToken.UserId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        var result = await handler.Handle(new ConfirmEmailCommand(rawToken), CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.IsActive),
            Arg.Any<CancellationToken>());
        await _tokenRepository.Received(1).UpdateAsync(
            Arg.Is<EmailConfirmationToken>(t => t.IsUsed()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenLookedUpByHash_NotByRawValue()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        var rawToken = "raw_token_that_must_not_be_stored";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var confirmationToken = EmailConfirmationToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(24));

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(confirmationToken);
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        await handler.Handle(new ConfirmEmailCommand(rawToken), CancellationToken.None);

        await _tokenRepository.Received(1).FindByTokenHashAsync(
            Arg.Is<string>(h => h == tokenHash && h != rawToken),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorizedException()
    {
        _tokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((EmailConfirmationToken?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ConfirmEmailCommand("nonexistent_token"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ThrowsUnauthorizedException()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        var rawToken = "already_used_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var usedToken = EmailConfirmationToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddHours(24));
        usedToken.MarkAsUsed();

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(usedToken);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new ConfirmEmailCommand(rawToken), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public void Validator_EmptyToken_ProducesError()
    {
        var validator = new ConfirmEmailCommandHandler.Validator();
        var result = validator.TestValidate(new ConfirmEmailCommand(""));
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validator_ValidToken_HasNoErrors()
    {
        var validator = new ConfirmEmailCommandHandler.Validator();
        var result = validator.TestValidate(new ConfirmEmailCommand("valid_token"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

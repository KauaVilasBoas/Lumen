using Lumen.CommandHandlers.Auth.ResendConfirmationEmail;
using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Auth.ResendConfirmationEmail;

public sealed class ResendConfirmationEmailCommandHandlerTests
{
    private const string ExistingEmail = "alice@example.com";
    private const string ExistingUsername = "alice";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IEmailConfirmationService _emailConfirmationService = Substitute.For<IEmailConfirmationService>();

    // ── Email not found — silent no-op (anti-enumeration) ─────────────────

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_CompletesWithoutError()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(new ResendConfirmationEmailCommandHandler.Command("nobody@example.com"), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_DoesNotSendEmail()
    {
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await CreateHandler().Handle(new ResendConfirmationEmailCommandHandler.Command("nobody@example.com"), CancellationToken.None);

        await _emailConfirmationService.DidNotReceive()
            .SendConfirmationEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── Already active user — silent no-op ────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_DoesNotSendEmail()
    {
        var activeUser = BuildConfirmedUser();

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(activeUser);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailConfirmationService.DidNotReceive()
            .SendConfirmationEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_DoesNotInvalidatePreviousTokens()
    {
        var activeUser = BuildConfirmedUser();

        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(activeUser);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.DidNotReceive().InvalidateByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsPending_InvalidatesPreviousTokens()
    {
        var user = BuildPendingUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InvalidateByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsPending_DelegatesToEmailConfirmationService()
    {
        var user = BuildPendingUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailConfirmationService.Received(1)
            .SendConfirmationEmailAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsPending_CompletesWithoutError()
    {
        var user = BuildPendingUser();
        _userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ResendConfirmationEmailCommandHandler CreateHandler() =>
        new(_userRepository, _tokenRepository, _emailConfirmationService);

    private static ResendConfirmationEmailCommandHandler.Command ValidCommand() =>
        new(ExistingEmail);

    private static User BuildPendingUser() =>
        User.Create(ExistingEmail, ExistingUsername, "$2a$12$fakehash");

    private static User BuildConfirmedUser()
    {
        var user = User.Create(ExistingEmail, ExistingUsername, "$2a$12$fakehash");
        user.ConfirmEmail();
        return user;
    }
}

using Lumen.CommandHandlers.Users.ChangePassword;
using Lumen.Domain.Notifications;
using Lumen.Domain.Security;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Users.ChangePassword;

public sealed class ChangePasswordCommandHandlerTests
{
    private const string ExistingEmail = "alice@example.com";
    private const string ExistingUsername = "alice";
    private const string CurrentPasswordPlain = "OldStr0ng!Pass";
    private const string CurrentPasswordHash = "$2a$12$oldhash";
    private const string NewPasswordPlain = "NewStr0ng!Pass";
    private const string NewPasswordHash = "$2a$12$newhash";

    private static readonly Guid UserId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IUserPasswordService _userPasswordService = Substitute.For<IUserPasswordService>();

    public ChangePasswordCommandHandlerTests()
    {
        _passwordHasher.Verify(CurrentPasswordPlain, CurrentPasswordHash).Returns(true);
        _passwordHasher.Verify(NewPasswordPlain, CurrentPasswordHash).Returns(false);
        _passwordHasher.Hash(NewPasswordPlain).Returns(NewPasswordHash);

        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);
    }

    // ── User not found ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Current password incorrect ─────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCurrentPasswordIncorrect_ThrowsValidationException()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(CurrentPasswordPlain, user.PasswordHash).Returns(false);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SharedKernel.Exceptions.ValidationException>();
    }

    // ── New password same as current ──────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNewPasswordSameAsCurrent_ThrowsValidationException()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(CurrentPasswordPlain, user.PasswordHash).Returns(true);
        _passwordHasher.Verify(NewPasswordPlain, user.PasswordHash).Returns(true);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SharedKernel.Exceptions.ValidationException>();
    }

    // ── Password policy failure ───────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNewPasswordFailsPolicy_ThrowsValidationException()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["Senha fraca."]));

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<SharedKernel.Exceptions.ValidationException>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPasswordValid_UpdatesPasswordHash()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        user.PasswordHash.Should().Be(NewPasswordHash);
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_RevokesRefreshTokensViaService()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _userPasswordService.Received(1)
            .RevokeAllRefreshTokensAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_SendsPasswordChangedEmailViaService()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _userPasswordService.Received(1)
            .SendPasswordChangedEmailAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_CompletesWithoutError()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_ValidatesAgainstUserIdentity()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _passwordValidator.Received(1).ValidatePasswordAsync(
            Arg.Is<PasswordValidationContext>(ctx =>
                ctx.Email == user.Email && ctx.Username == user.Username),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private ChangePasswordCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _passwordHasher,
            _passwordValidator,
            _userPasswordService,
            NullLogger<ChangePasswordCommandHandler>.Instance);

    private static ChangePasswordCommandHandler.Command ValidCommand() =>
        new(UserId, CurrentPasswordPlain, NewPasswordPlain);

    private static User BuildUser()
    {
        var user = User.Create(ExistingEmail, ExistingUsername, CurrentPasswordHash);
        return user;
    }
}

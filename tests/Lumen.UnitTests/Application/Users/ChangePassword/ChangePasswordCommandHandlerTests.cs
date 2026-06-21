using Lumen.CommandHandlers.Users.ChangePassword;
using Lumen.Domain.Notifications;
using Lumen.Domain.Security;
using Lumen.Domain.Tokens;
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

    private readonly IRefreshTokenRepository _refreshTokenRepository =
        Substitute.For<IRefreshTokenRepository>();

    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();

    public ChangePasswordCommandHandlerTests()
    {
        _passwordHasher.Verify(CurrentPasswordPlain, CurrentPasswordHash).Returns(true);
        _passwordHasher.Verify(NewPasswordPlain, CurrentPasswordHash).Returns(false);
        _passwordHasher.Hash(NewPasswordPlain).Returns(NewPasswordHash);

        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);

        _refreshTokenRepository
            .FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RefreshToken>());

        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "txt"));
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
    public async Task Handle_WhenPasswordValid_RevokesActiveRefreshTokens()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var activeToken = RefreshToken.Create(user.Id, "hash", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        _refreshTokenRepository
            .FindByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { activeToken });

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        activeToken.IsRevoked().Should().BeTrue();
        await _refreshTokenRepository.Received(1).UpdateAsync(activeToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_SendsPasswordChangedEmail()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == user.Email),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_RendersPasswordChangedTemplate()
    {
        var user = BuildUser();
        _userRepository.FindByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            "PasswordChanged",
            Arg.Any<IReadOnlyDictionary<string, string>>());
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
            _refreshTokenRepository,
            _passwordHasher,
            _passwordValidator,
            _emailService,
            _templateRenderer,
            NullLogger<ChangePasswordCommandHandler>.Instance);

    private static ChangePasswordCommandHandler.Command ValidCommand() =>
        new(UserId, CurrentPasswordPlain, NewPasswordPlain);

    private static User BuildUser()
    {
        var user = User.Create(ExistingEmail, ExistingUsername, CurrentPasswordHash);
        return user;
    }
}

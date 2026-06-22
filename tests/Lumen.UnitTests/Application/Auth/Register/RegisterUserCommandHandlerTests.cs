using Lumen.CommandHandlers.Auth.Register;
using Lumen.Domain.Configuration;
using Lumen.Domain.Notifications;
using Lumen.Domain.Security;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.UnitTests.Application.Auth.Register;

public sealed class RegisterUserCommandHandlerTests
{
    private const string ValidEmail = "alice@example.com";
    private const string ValidUsername = "alice";
    private const string ValidPassword = "Str0ng!Passw0rd-2026";
    private const string FakeHash = "$2a$12$fakehash";
    private const string FakeBaseUrl = "https://api.example.com";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();
    private readonly IAppSettings _appSettings = Substitute.For<IAppSettings>();

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakeHash);
        _passwordValidator.ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);
        _appSettings.BaseUrl.Returns(FakeBaseUrl);
        _templateRenderer.Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html>confirm</html>", "confirm"));
    }

    // ── Password validation ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenPasswordIsWeak_ThrowsValidationException()
    {
        var errors = new[] { "A senha deve ter no mínimo 12 caracteres." };
        _passwordValidator.ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(errors));

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.ContainsKey("password") &&
                         ex.Errors["password"].SequenceEqual(errors));
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWeak_DoesNotInsertUser()
    {
        _passwordValidator.ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["too short"]));

        await Assert.ThrowsAsync<ValidationException>(
            () => CreateHandler().Handle(ValidCommand(), CancellationToken.None));

        await _userRepository.DidNotReceive().InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── Duplicate conflicts ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailIsDuplicate_ThrowsConflictException()
    {
        _userRepository.InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEmailException(ValidEmail));

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenUsernameIsDuplicate_ThrowsConflictException()
    {
        _userRepository.InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateUsernameException(ValidUsername));

        var act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCommandIsValid_ReturnsSuccessWithUserData()
    {
        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Email.Should().Be(User.NormalizeEmail(ValidEmail));
        result.Username.Should().Be(ValidUsername);
        result.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_HashesPasswordBeforeInserting()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _passwordHasher.Received(1).Hash(ValidPassword);
        await _userRepository.Received(1).InsertAsync(
            Arg.Is<User>(u => u.PasswordHash == FakeHash),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_CreatesUserWithIsActiveFalse()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _userRepository.Received(1).InsertAsync(
            Arg.Is<User>(u => u.IsActive == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_InsertsEmailConfirmationToken()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Any<EmailConfirmationToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_SendsConfirmationEmail()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == User.NormalizeEmail(ValidEmail)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_ConfirmationUrlContainsBaseUrl()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _templateRenderer.Received(1).Render(
            "EmailConfirmation",
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey("ConfirmationUrl") &&
                d["ConfirmationUrl"].StartsWith(FakeBaseUrl)));
    }

    [Fact]
    public async Task Handle_WhenCommandIsValid_PassesEmailAndUsernameToPasswordValidator()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _passwordValidator.Received(1).ValidatePasswordAsync(
            Arg.Is<PasswordValidationContext>(ctx =>
                ctx.Email == ValidEmail && ctx.Username == ValidUsername),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private RegisterUserCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _tokenRepository,
            _passwordHasher,
            _passwordValidator,
            _emailService,
            _templateRenderer,
            _appSettings,
            NullLogger<RegisterUserCommandHandler>.Instance);

    private static RegisterUserCommandHandler.Command ValidCommand() =>
        new(ValidEmail, ValidUsername, ValidPassword);
}

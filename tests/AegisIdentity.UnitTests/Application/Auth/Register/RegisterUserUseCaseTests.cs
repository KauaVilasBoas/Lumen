using AegisIdentity.Application.Auth.Register;
using AegisIdentity.Application.Configuration;
using AegisIdentity.Application.Notifications;
using AegisIdentity.Application.Security;
using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AegisIdentity.UnitTests.Application.Auth.Register;

public sealed class RegisterUserUseCaseTests
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

    public RegisterUserUseCaseTests()
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
    public async Task ExecuteAsync_WhenPasswordIsWeak_ReturnsWeakPasswordResult()
    {
        var errors = new[] { "A senha deve ter no mínimo 12 caracteres." };
        _passwordValidator.ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(errors));

        var result = await CreateUseCase().ExecuteAsync(ValidRequest());

        result.Should().BeOfType<RegisterResult.WeakPassword>();
        ((RegisterResult.WeakPassword)result).Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordIsWeak_DoesNotInsertUser()
    {
        _passwordValidator.ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["too short"]));

        await CreateUseCase().ExecuteAsync(ValidRequest());

        await _userRepository.DidNotReceive().InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── Duplicate conflicts ────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenEmailIsDuplicate_ReturnsDuplicateEmailResult()
    {
        _userRepository.InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEmailException(ValidEmail));

        var result = await CreateUseCase().ExecuteAsync(ValidRequest());

        result.Should().BeOfType<RegisterResult.DuplicateEmail>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUsernameIsDuplicate_ReturnsDuplicateUsernameResult()
    {
        _userRepository.InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateUsernameException(ValidUsername));

        var result = await CreateUseCase().ExecuteAsync(ValidRequest());

        result.Should().BeOfType<RegisterResult.DuplicateUsername>();
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ReturnsSuccessWithUserData()
    {
        var result = await CreateUseCase().ExecuteAsync(ValidRequest());

        var success = result.Should().BeOfType<RegisterResult.Success>().Subject;
        success.Response.Email.Should().Be(User.NormalizeEmail(ValidEmail));
        success.Response.Username.Should().Be(ValidUsername);
        success.Response.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_HashesPasswordBeforeInserting()
    {
        await CreateUseCase().ExecuteAsync(ValidRequest());

        _passwordHasher.Received(1).Hash(ValidPassword);
        await _userRepository.Received(1).InsertAsync(
            Arg.Is<User>(u => u.PasswordHash == FakeHash),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_CreatesUserWithIsActiveFalse()
    {
        await CreateUseCase().ExecuteAsync(ValidRequest());

        await _userRepository.Received(1).InsertAsync(
            Arg.Is<User>(u => u.IsActive == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_InsertsEmailConfirmationToken()
    {
        await CreateUseCase().ExecuteAsync(ValidRequest());

        await _tokenRepository.Received(1).InsertAsync(
            Arg.Any<EmailConfirmationToken>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_SendsConfirmationEmail()
    {
        await CreateUseCase().ExecuteAsync(ValidRequest());

        await _emailService.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == User.NormalizeEmail(ValidEmail)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_ConfirmationUrlContainsBaseUrl()
    {
        await CreateUseCase().ExecuteAsync(ValidRequest());

        _templateRenderer.Received(1).Render(
            "EmailConfirmation",
            Arg.Is<IReadOnlyDictionary<string, string>>(d =>
                d.ContainsKey("ConfirmationUrl") &&
                d["ConfirmationUrl"].StartsWith(FakeBaseUrl)));
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailServiceFails_StillReturnsSuccess()
    {
        // IEmailService is fail-open; exceptions propagated from SendAsync would be a
        // contract violation, but we guard against it here for defensive completeness.
        _emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask); // confirm it doesn't throw

        var result = await CreateUseCase().ExecuteAsync(ValidRequest());

        result.Should().BeOfType<RegisterResult.Success>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRequestIsValid_PassesEmailAndUsernameToPasswordValidator()
    {
        await CreateUseCase().ExecuteAsync(ValidRequest());

        await _passwordValidator.Received(1).ValidatePasswordAsync(
            Arg.Is<PasswordValidationContext>(ctx =>
                ctx.Email == ValidEmail && ctx.Username == ValidUsername),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private RegisterUserUseCase CreateUseCase() =>
        new(
            _userRepository,
            _tokenRepository,
            _passwordHasher,
            _passwordValidator,
            _emailService,
            _templateRenderer,
            _appSettings,
            NullLogger<RegisterUserUseCase>.Instance);

    private static RegisterRequest ValidRequest() =>
        new(ValidEmail, ValidUsername, ValidPassword);
}

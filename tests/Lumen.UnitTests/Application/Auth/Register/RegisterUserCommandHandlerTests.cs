using Lumen.CommandHandlers.Auth.Register;
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

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IEmailConfirmationService _emailConfirmationService = Substitute.For<IEmailConfirmationService>();

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns(FakeHash);
        _passwordValidator.ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);
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
    public async Task Handle_WhenCommandIsValid_DelegatesToEmailConfirmationService()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await _emailConfirmationService.Received(1)
            .SendConfirmationEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
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
            _passwordHasher,
            _passwordValidator,
            _emailConfirmationService,
            NullLogger<RegisterUserCommandHandler>.Instance);

    private static RegisterUserCommandHandler.Command ValidCommand() =>
        new(ValidEmail, ValidUsername, ValidPassword);
}

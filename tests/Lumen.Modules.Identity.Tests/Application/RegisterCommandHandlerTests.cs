using FluentAssertions;
using Lumen.Modules.Identity.Application.Auth.Register;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class RegisterCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IEmailConfirmationService _emailConfirmationService = Substitute.For<IEmailConfirmationService>();

    private RegisterCommandHandler CreateHandler()
        => new(
            _userRepository,
            _passwordHasher,
            _passwordValidator,
            _emailConfirmationService,
            NullLogger<RegisterCommandHandler>.Instance);

    private void SetupValidPassword()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed_password");
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndSendsConfirmationEmail()
    {
        SetupValidPassword();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RegisterCommand("alice@test.com", "alice", "StrongPass1!"),
            CancellationToken.None);

        result.Email.Should().Be("alice@test.com");
        result.Username.Should().Be("alice");

        await _userRepository.Received(1).InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _emailConfirmationService.Received(1).SendConfirmationEmailAsync(
            Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WeakPassword_ThrowsValidationException()
    {
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["Password is too weak."]));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RegisterCommand("alice@test.com", "alice", "weak"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _userRepository.DidNotReceive().InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        SetupValidPassword();
        _userRepository
            .InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEmailException("alice@test.com"));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RegisterCommand("alice@test.com", "alice", "StrongPass1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsConflictException()
    {
        SetupValidPassword();
        _userRepository
            .InsertAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateUsernameException("alice"));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RegisterCommand("alice@test.com", "alice", "StrongPass1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}

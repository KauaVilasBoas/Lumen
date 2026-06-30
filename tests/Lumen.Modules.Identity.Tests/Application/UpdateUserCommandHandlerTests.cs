using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Users.Update;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class UpdateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IEmailConfirmationService _emailConfirmationService = Substitute.For<IEmailConfirmationService>();

    private UpdateUserCommandHandler CreateHandler()
        => new(
            _userRepository,
            _tokenRepository,
            _emailConfirmationService,
            NullLogger<UpdateUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ChangeUsername_UpdatesUserAndReturnsEmailChangedFalse()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository
            .FindByUsernameAsync("new_alice", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, null, "new_alice", "admin"),
            CancellationToken.None);

        result.EmailChanged.Should().BeFalse();
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.Username == "new_alice"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChangeEmail_RequiresReconfirmationAndSendsEmail()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, "new@test.com", null, "admin"),
            CancellationToken.None);

        result.EmailChanged.Should().BeTrue();
        await _tokenRepository.Received(1).InvalidateByUserIdAsync(user.Id, Arg.Any<CancellationToken>());
        await _emailConfirmationService.Received(1).SendConfirmationEmailAsync(
            Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoChanges_ReturnsFalseWithoutUpdating()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UpdateUserCommand(user.Id, null, null, "admin"),
            CancellationToken.None);

        result.EmailChanged.Should().BeFalse();
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository
            .FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _userRepository
            .UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEmailException("new@test.com"));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new UpdateUserCommand(user.Id, "new@test.com", null, "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_DuplicateUsername_ThrowsConflictException()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        var conflictUser = User.Create("bob@test.com", "new_alice", "hash2");

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userRepository
            .FindByUsernameAsync("new_alice", Arg.Any<CancellationToken>())
            .Returns(conflictUser);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new UpdateUserCommand(user.Id, null, "new_alice", "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "new@test.com", null, "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Validator_InvalidEmail_ProducesError()
    {
        var validator = new UpdateUserCommandHandler.Validator();
        var result = validator.TestValidate(
            new UpdateUserCommand(Guid.NewGuid(), "not-an-email", null, "admin"));
        result.ShouldHaveValidationErrorFor(x => x.NewEmail);
    }

    [Fact]
    public void Validator_UsernameTooShort_ProducesError()
    {
        var validator = new UpdateUserCommandHandler.Validator();
        var result = validator.TestValidate(
            new UpdateUserCommand(Guid.NewGuid(), null, "ab", "admin"));
        result.ShouldHaveValidationErrorFor(x => x.NewUsername);
    }

    [Fact]
    public void Validator_NullEmailAndUsername_HasNoErrors()
    {
        var validator = new UpdateUserCommandHandler.Validator();
        var result = validator.TestValidate(
            new UpdateUserCommand(Guid.NewGuid(), null, null, "admin"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

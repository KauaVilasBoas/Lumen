using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Users.ChangePassword;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class ChangePasswordCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordChangedNotificationService _passwordChangedNotificationService =
        Substitute.For<IPasswordChangedNotificationService>();

    private ChangePasswordCommandHandler CreateHandler()
        => new(
            _userRepository,
            _passwordHasher,
            _passwordValidator,
            _refreshTokenRepository,
            _passwordChangedNotificationService,
            NullLogger<ChangePasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidChange_UpdatesPasswordRevokesTokensAndSendsEmail()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CurrentPass1!", "old_hash").Returns(true);
        _passwordHasher.Verify("NewPass1!", "old_hash").Returns(false);
        _passwordHasher.Hash("NewPass1!").Returns("new_hash");
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ChangePasswordCommand(user.Id, "CurrentPass1!", "NewPass1!"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.PasswordHash == "new_hash"),
            Arg.Any<CancellationToken>());
        await _passwordChangedNotificationService.Received(1).SendPasswordChangedEmailAsync(
            user,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidChange_RevokesAllActiveRefreshTokens()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CurrentPass1!", "old_hash").Returns(true);
        _passwordHasher.Verify("NewPass1!", "old_hash").Returns(false);
        _passwordHasher.Hash("NewPass1!").Returns("new_hash");
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);

        var handler = CreateHandler();
        await handler.Handle(
            new ChangePasswordCommand(user.Id, "CurrentPass1!", "NewPass1!"),
            CancellationToken.None);

        await _refreshTokenRepository.Received(1).RevokeAllActiveByUserIdAsync(
            user.Id,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsValidationException()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("WrongPass!", "old_hash").Returns(false);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ChangePasswordCommand(user.Id, "WrongPass!", "NewPass1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_NewPasswordSameAsCurrent_ThrowsValidationException()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CurrentPass1!", "old_hash").Returns(true);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ChangePasswordCommand(user.Id, "CurrentPass1!", "CurrentPass1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository
            .FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ChangePasswordCommand(Guid.NewGuid(), "CurrentPass1!", "NewPass1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WeakNewPassword_ThrowsValidationException()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();

        _userRepository.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("CurrentPass1!", "old_hash").Returns(true);
        _passwordHasher.Verify("weak", "old_hash").Returns(false);
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["Password is too weak."]));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ChangePasswordCommand(user.Id, "CurrentPass1!", "weak"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public void Validator_EmptyCurrentPassword_ProducesError()
    {
        var validator = new ChangePasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "", "NewPass1!"));
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void Validator_EmptyNewPassword_ProducesError()
    {
        var validator = new ChangePasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ChangePasswordCommand(Guid.NewGuid(), "CurrentPass1!", ""));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        var validator = new ChangePasswordCommandHandler.Validator();
        var result = validator.TestValidate(
            new ChangePasswordCommand(Guid.NewGuid(), "CurrentPass1!", "NewPass1!"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Auth.ResetPassword;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class ResetPasswordCommandHandlerTests
{
    private readonly IPasswordResetTokenRepository _tokenRepository = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IPasswordValidator _passwordValidator = Substitute.For<IPasswordValidator>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateRenderer _templateRenderer = Substitute.For<IEmailTemplateRenderer>();

    private ResetPasswordCommandHandler CreateHandler()
        => new(
            _tokenRepository,
            _userRepository,
            _passwordHasher,
            _passwordValidator,
            _refreshTokenRepository,
            _emailService,
            _templateRenderer,
            NullLogger<ResetPasswordCommandHandler>.Instance);

    private void SetupValidPassword(string newPassword = "NewStrongPass1!")
    {
        _passwordHasher.Hash(newPassword).Returns("new_hashed_password");
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Success);
        _templateRenderer
            .Render(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(("<html/>", "text"));
    }

    [Fact]
    public async Task Handle_ValidToken_ChangesPasswordRevokesTokensAndSendsEmail()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();
        var rawToken = "valid_raw_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddMinutes(30));

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(resetToken);
        _userRepository.FindByIdAsync(resetToken.UserId, Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokenRepository.FindByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);
        SetupValidPassword();

        var handler = CreateHandler();
        var result = await handler.Handle(
            new ResetPasswordCommand(rawToken, "NewStrongPass1!"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.PasswordHash == "new_hashed_password"),
            Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidReset_TokenLookedUpByHash_NotByRawValue()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();
        var rawToken = "raw_token_that_must_not_be_stored";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddMinutes(30));

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(resetToken);
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokenRepository.FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns([]);
        SetupValidPassword();

        var handler = CreateHandler();
        await handler.Handle(new ResetPasswordCommand(rawToken, "NewStrongPass1!"), CancellationToken.None);

        await _tokenRepository.Received(1).FindByTokenHashAsync(
            Arg.Is<string>(h => h == tokenHash && h != rawToken),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidReset_RevokesAllActiveRefreshTokens()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();
        var rawToken = "valid_raw_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddMinutes(30));

        var activeRefreshToken = RefreshToken.Create(
            user.Id, "refresh_hash", DateTime.UtcNow.AddDays(7), "127.0.0.1");

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(resetToken);
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
        _refreshTokenRepository
            .FindByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns([activeRefreshToken]);
        SetupValidPassword();

        var handler = CreateHandler();
        await handler.Handle(new ResetPasswordCommand(rawToken, "NewStrongPass1!"), CancellationToken.None);

        await _refreshTokenRepository.Received(1).UpdateAsync(
            Arg.Is<RefreshToken>(t => t.IsRevoked()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsUnauthorizedException()
    {
        _tokenRepository
            .FindByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ResetPasswordCommand("nonexistent_token", "NewPass1!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WeakNewPassword_ThrowsValidationException()
    {
        var user = User.Create("alice@test.com", "alice", "old_hash");
        user.ConfirmEmail();
        var rawToken = "valid_raw_token";
        var tokenHash = Sha256Hasher.ComputeHex(rawToken);
        var resetToken = PasswordResetToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddMinutes(30));

        _tokenRepository.FindByTokenHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(resetToken);
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordValidator
            .ValidatePasswordAsync(Arg.Any<PasswordValidationContext>(), Arg.Any<CancellationToken>())
            .Returns(PasswordValidationResult.Failure(["Password is too weak."]));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new ResetPasswordCommand(rawToken, "weak"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyToken_ProducesError()
    {
        var validator = new ResetPasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ResetPasswordCommand("", "NewPass1!"));
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validator_EmptyNewPassword_ProducesError()
    {
        var validator = new ResetPasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ResetPasswordCommand("token", ""));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
        var validator = new ResetPasswordCommandHandler.Validator();
        var result = validator.TestValidate(new ResetPasswordCommand("valid_token", "NewPass1!"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

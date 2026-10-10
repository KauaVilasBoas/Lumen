using FluentValidation;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Unit>;

internal sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, Unit>
{
    public sealed class Validator : AbstractValidator<ResetPasswordCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(AuthErrorMessages.TokenRequired);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(AuthErrorMessages.NewPasswordRequired);
        }
    }

    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordChangedNotificationService _passwordChangedNotificationService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordChangedNotificationService passwordChangedNotificationService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordChangedNotificationService = passwordChangedNotificationService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var tokenHash = Sha256Hasher.ComputeHex(cmd.Token);
        var resetToken = await _tokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (resetToken is null || !resetToken.IsValid())
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredToken);

        var user = await _userRepository.FindByIdAsync(resetToken.UserId, ct);

        if (user is null)
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredToken);

        var passwordValidation = await _passwordValidator.ValidatePasswordAsync(
            new(cmd.NewPassword, user.Email, user.Username), ct);

        if (!passwordValidation.IsValid)
            throw new SharedKernel.Exceptions.ValidationException("newPassword", passwordValidation.Errors);

        resetToken.MarkAsUsed();
        await _tokenRepository.UpdateAsync(resetToken, ct);

        user.ChangePassword(_passwordHasher.Hash(cmd.NewPassword));
        await _userRepository.UpdateAsync(user, ct);

        await _refreshTokenRepository.RevokeAllActiveByUserIdAsync(user.Id, ct);

        _logger.LogInformation("Password reset completed for UserId {UserId}", user.Id);

        await _passwordChangedNotificationService.SendPasswordChangedEmailAsync(user, ct);

        return Unit.Value;
    }
}

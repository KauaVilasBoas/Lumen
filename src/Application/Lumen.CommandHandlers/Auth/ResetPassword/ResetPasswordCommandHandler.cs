using Lumen.Domain.Security;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.CommandHandlers.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommandHandler.Command, Unit>
{
    public sealed record Command(string Token, string NewPassword) : IRequest<Unit>;

    public sealed class Validator : AbstractValidator<Command>
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
    private readonly IUserPasswordService _userPasswordService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IUserPasswordService userPasswordService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _userPasswordService = userPasswordService;
        _logger = logger;
    }

    public async Task<Unit> Handle(Command cmd, CancellationToken ct)
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

        await _userPasswordService.RevokeAllRefreshTokensAsync(user.Id, ct);

        _logger.LogInformation("Password reset completed for UserId {UserId}", user.Id);

        await _userPasswordService.SendPasswordChangedEmailAsync(user, ct);

        return Unit.Value;
    }
}

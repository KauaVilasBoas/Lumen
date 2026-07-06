using FluentValidation;
using Lumen.Identity.Domain.Notifications;
using Lumen.Identity.Domain.Security;
using Lumen.Identity.Domain.Tokens;
using Lumen.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Identity.Application.Auth.ResetPassword;

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
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _refreshTokenRepository = refreshTokenRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
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

        await RevokeAllRefreshTokensAsync(user.Id, ct);

        _logger.LogInformation("Password reset completed for UserId {UserId}", user.Id);

        await SendPasswordChangedEmailAsync(user, ct);

        return Unit.Value;
    }

    private async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var tokens = await _refreshTokenRepository.FindByUserIdAsync(userId, ct);

        foreach (var token in tokens.Where(t => t.IsActive()))
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token, ct);
        }
    }

    private async Task SendPasswordChangedEmailAsync(User user, CancellationToken ct)
    {
        var placeholders = new Dictionary<string, string>
        {
            [EmailPlaceholderKeys.UserName] = user.Username,
        };

        var (htmlBody, textBody) = _templateRenderer.Render(EmailTemplateNames.PasswordChanged, placeholders);

        var message = new EmailMessage(
            To: user.Email,
            Subject: EmailSubjects.PasswordChanged,
            HtmlBody: htmlBody,
            TextBody: textBody);

        await _emailService.SendAsync(message, ct);
    }
}

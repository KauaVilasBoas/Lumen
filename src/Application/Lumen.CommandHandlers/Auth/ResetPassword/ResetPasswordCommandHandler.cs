using AegisIdentity.Domain.Notifications;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Constants;
using AegisIdentity.SharedKernel.Exceptions;
using AegisIdentity.SharedKernel.Util;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Auth.ResetPassword;

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
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
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

        user.PasswordHash = _passwordHasher.Hash(cmd.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
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

using Lumen.Domain.Notifications;
using Lumen.Domain.Security;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.CommandHandlers.Users.ChangePassword;

public sealed class ChangePasswordCommandHandler
    : IRequestHandler<ChangePasswordCommandHandler.Command, Unit>
{
    public sealed record Command(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Unit>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(AuthErrorMessages.CurrentPasswordRequired);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(AuthErrorMessages.NewPasswordRequired);
        }
    }

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IEmailService emailService,
        IEmailTemplateRenderer templateRenderer,
        ILogger<ChangePasswordCommandHandler> logger)
    {
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
        var user = await _userRepository.FindByIdAsync(cmd.UserId, ct);

        if (user is null)
            throw new NotFoundException(AuthErrorMessages.UserNotFound);

        if (!_passwordHasher.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new SharedKernel.Exceptions.ValidationException(
                "currentPassword",
                [AuthErrorMessages.CurrentPasswordIncorrect]);

        if (_passwordHasher.Verify(cmd.NewPassword, user.PasswordHash))
            throw new SharedKernel.Exceptions.ValidationException(
                "newPassword",
                [AuthErrorMessages.NewPasswordSameAsCurrent]);

        var passwordValidation = await _passwordValidator.ValidatePasswordAsync(
            new(cmd.NewPassword, user.Email, user.Username), ct);

        if (!passwordValidation.IsValid)
            throw new SharedKernel.Exceptions.ValidationException("newPassword", passwordValidation.Errors);

        user.PasswordHash = _passwordHasher.Hash(cmd.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        await RevokeAllRefreshTokensAsync(user.Id, ct);

        _logger.LogInformation("Password changed for UserId {UserId}", user.Id);

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

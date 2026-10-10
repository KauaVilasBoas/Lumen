using FluentValidation;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Users.Update;

public sealed record UpdateUserCommand(
    Guid UserId,
    string? NewEmail,
    string? NewUsername,
    string ActorId) : IRequest<UpdateUserResult>;

public sealed record UpdateUserResult(Guid UserId, bool EmailChanged);

internal sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, UpdateUserResult>
{
    public sealed class Validator : AbstractValidator<UpdateUserCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NewEmail)
                .EmailAddress().WithMessage(AuthErrorMessages.EmailInvalid)
                .MaximumLength(ValidationLimits.EmailMaxLength).WithMessage(AuthErrorMessages.EmailTooLong)
                .When(x => !string.IsNullOrWhiteSpace(x.NewEmail));

            RuleFor(x => x.NewUsername)
                .MinimumLength(ValidationLimits.UsernameMinLength)
                    .WithMessage(string.Format(AuthErrorMessages.UsernameTooShort, ValidationLimits.UsernameMinLength))
                .MaximumLength(ValidationLimits.UsernameMaxLength)
                    .WithMessage(string.Format(AuthErrorMessages.UsernameTooLong, ValidationLimits.UsernameMaxLength))
                .Matches(ValidationLimits.UsernameAllowedCharsPattern)
                    .WithMessage(AuthErrorMessages.UsernameInvalidChars)
                .When(x => !string.IsNullOrWhiteSpace(x.NewUsername));
        }
    }

    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IEmailConfirmationService _emailConfirmationService;
    private readonly ILogger<UpdateUserCommandHandler> _logger;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository tokenRepository,
        IEmailConfirmationService emailConfirmationService,
        ILogger<UpdateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailConfirmationService = emailConfirmationService;
        _logger = logger;
    }

    public async Task<UpdateUserResult> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException(AuthErrorMessages.UserNotFound);

        var changedFields = new List<string>();

        var usernameEntry = await ApplyUsernameChangeIfRequested(cmd, user, ct);
        if (usernameEntry is not null)
            changedFields.Add(usernameEntry);

        var emailEntry = await ApplyEmailChangeIfRequested(cmd, user, ct);
        var emailChanged = emailEntry is not null;
        if (emailChanged)
            changedFields.Add(emailEntry!);

        if (changedFields.Count == 0)
            return new UpdateUserResult(user.Id, EmailChanged: false);

        try
        {
            await _userRepository.UpdateAsync(user, ct);
        }
        catch (DuplicateEmailException)
        {
            throw new ConflictException(AuthErrorMessages.EmailAlreadyInUse);
        }
        catch (DuplicateUsernameException)
        {
            throw new ConflictException(AuthErrorMessages.UsernameAlreadyInUse);
        }

        _logger.LogInformation(
            "User {UserId} updated by actor {ActorId}. Changes: {Changes}",
            user.Id, cmd.ActorId, string.Join(", ", changedFields));

        if (emailChanged)
        {
            await _tokenRepository.InvalidateByUserIdAsync(user.Id, ct);
            await _emailConfirmationService.SendConfirmationEmailAsync(user, ct);
        }

        return new UpdateUserResult(user.Id, emailChanged);
    }

    private async Task<string?> ApplyUsernameChangeIfRequested(UpdateUserCommand cmd, User user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NewUsername))
            return null;

        if (string.Equals(cmd.NewUsername, user.Username, StringComparison.Ordinal))
            return null;

        var existing = await _userRepository.FindByUsernameAsync(cmd.NewUsername, ct);
        if (existing is not null && existing.Id != user.Id)
            throw new ConflictException(AuthErrorMessages.UsernameAlreadyInUse);

        var logEntry = string.Format(AuditMessageTemplates.UsernameChangedEntry, user.Username, cmd.NewUsername);
        user.ChangeUsername(cmd.NewUsername);
        return logEntry;
    }

    private async Task<string?> ApplyEmailChangeIfRequested(UpdateUserCommand cmd, User user, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.NewEmail))
            return null;

        var normalizedNewEmail = User.NormalizeEmail(cmd.NewEmail);
        if (string.Equals(normalizedNewEmail, user.Email, StringComparison.Ordinal))
            return null;

        var existing = await _userRepository.FindByEmailAsync(normalizedNewEmail, ct);
        if (existing is not null && existing.Id != user.Id)
            throw new ConflictException(AuthErrorMessages.EmailAlreadyInUse);

        var logEntry = string.Format(AuditMessageTemplates.EmailChangedEntry, user.Email, normalizedNewEmail);
        user.ChangeEmail(cmd.NewEmail);
        return logEntry;
    }
}

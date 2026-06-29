using FluentValidation;
using Lumen.Modules.Identity.Domain.Notifications;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Users.Update;

internal sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommandHandler.Command, UpdateUserCommandHandler.Result>
{
    public sealed record Command(
        Guid UserId,
        string? NewEmail,
        string? NewUsername,
        string ActorId) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
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

    public sealed record Result(Guid UserId, bool EmailChanged);

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

    public async Task<Result> Handle(Command cmd, CancellationToken ct)
    {
        var user = await _userRepository.FindByIdAsync(cmd.UserId, ct)
            ?? throw new NotFoundException(AuthErrorMessages.UserNotFound);

        var emailChanged = false;
        var changedFields = new List<string>();

        if (!string.IsNullOrWhiteSpace(cmd.NewUsername) &&
            !string.Equals(cmd.NewUsername, user.Username, StringComparison.Ordinal))
        {
            var existing = await _userRepository.FindByUsernameAsync(cmd.NewUsername, ct);
            if (existing is not null && existing.Id != user.Id)
                throw new ConflictException(AuthErrorMessages.UsernameAlreadyInUse);

            changedFields.Add($"username: '{user.Username}' → '{cmd.NewUsername}'");
            user.ChangeUsername(cmd.NewUsername);
        }

        if (!string.IsNullOrWhiteSpace(cmd.NewEmail))
        {
            var normalizedNewEmail = User.NormalizeEmail(cmd.NewEmail);
            if (!string.Equals(normalizedNewEmail, user.Email, StringComparison.Ordinal))
            {
                var existing = await _userRepository.FindByEmailAsync(normalizedNewEmail, ct);
                if (existing is not null && existing.Id != user.Id)
                    throw new ConflictException(AuthErrorMessages.EmailAlreadyInUse);

                changedFields.Add($"email: '{user.Email}' → '{normalizedNewEmail}'");
                user.ChangeEmail(cmd.NewEmail);
                emailChanged = true;
            }
        }

        if (changedFields.Count == 0)
            return new Result(user.Id, EmailChanged: false);

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

        return new Result(user.Id, emailChanged);
    }
}

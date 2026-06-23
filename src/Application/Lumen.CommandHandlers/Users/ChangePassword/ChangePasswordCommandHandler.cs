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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IUserPasswordService _userPasswordService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator,
        IUserPasswordService userPasswordService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
        _userPasswordService = userPasswordService;
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

        user.ChangePassword(_passwordHasher.Hash(cmd.NewPassword));
        await _userRepository.UpdateAsync(user, ct);

        await _userPasswordService.RevokeAllRefreshTokensAsync(user.Id, ct);

        _logger.LogInformation("Password changed for UserId {UserId}", user.Id);

        await _userPasswordService.SendPasswordChangedEmailAsync(user, ct);

        return Unit.Value;
    }
}

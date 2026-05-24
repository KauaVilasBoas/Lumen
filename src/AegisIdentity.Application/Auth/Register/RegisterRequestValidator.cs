using AegisIdentity.SharedKernel.Constants;
using FluentValidation;

namespace AegisIdentity.Application.Auth.Register;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage(AuthErrorMessages.EmailRequired)
            .EmailAddress().WithMessage(AuthErrorMessages.EmailInvalid)
            .MaximumLength(ValidationLimits.EmailMaxLength).WithMessage(AuthErrorMessages.EmailTooLong);

        RuleFor(r => r.Username)
            .NotEmpty().WithMessage(AuthErrorMessages.UsernameRequired)
            .MinimumLength(ValidationLimits.UsernameMinLength)
                .WithMessage(string.Format(AuthErrorMessages.UsernameTooShort, ValidationLimits.UsernameMinLength))
            .MaximumLength(ValidationLimits.UsernameMaxLength)
                .WithMessage(string.Format(AuthErrorMessages.UsernameTooLong, ValidationLimits.UsernameMaxLength))
            .Matches("^[a-zA-Z0-9_-]+$")
                .WithMessage(AuthErrorMessages.UsernameInvalidChars);

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage(AuthErrorMessages.PasswordRequired);
    }
}

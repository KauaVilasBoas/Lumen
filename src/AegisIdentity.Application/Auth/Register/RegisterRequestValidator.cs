using FluentValidation;

namespace AegisIdentity.Application.Auth.Register;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private const int UsernameMinLength = 3;
    private const int UsernameMaxLength = 32;

    public RegisterRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("O campo email é obrigatório.")
            .EmailAddress().WithMessage("O email informado não é válido.")
            .MaximumLength(256).WithMessage("O email deve ter no máximo 256 caracteres.");

        RuleFor(r => r.Username)
            .NotEmpty().WithMessage("O campo username é obrigatório.")
            .MinimumLength(UsernameMinLength)
                .WithMessage($"O username deve ter no mínimo {UsernameMinLength} caracteres.")
            .MaximumLength(UsernameMaxLength)
                .WithMessage($"O username deve ter no máximo {UsernameMaxLength} caracteres.")
            .Matches("^[a-zA-Z0-9_-]+$")
                .WithMessage("O username deve conter apenas letras, números, underscores ou hífens.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("O campo senha é obrigatório.");
    }
}

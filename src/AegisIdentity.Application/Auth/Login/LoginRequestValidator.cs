using FluentValidation;

namespace AegisIdentity.Application.Auth.Login;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Identifier)
            .NotEmpty().WithMessage("O campo identificador é obrigatório.");

        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("O campo senha é obrigatório.");
    }
}

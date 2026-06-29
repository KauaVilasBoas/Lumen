using FluentValidation;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;

namespace Lumen.Modules.Identity.Application.Auth.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler
    : IRequestHandler<ConfirmEmailCommandHandler.Command, Unit>
{
    public sealed record Command(string Token) : IRequest<Unit>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage(AuthErrorMessages.TokenRequired);
        }
    }

    private readonly IEmailConfirmationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;

    public ConfirmEmailCommandHandler(
        IEmailConfirmationTokenRepository tokenRepository,
        IUserRepository userRepository)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(Command cmd, CancellationToken ct)
    {
        var tokenHash = Sha256Hasher.ComputeHex(cmd.Token);
        var confirmationToken = await _tokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (confirmationToken is null || !confirmationToken.IsValid())
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredToken);

        var user = await _userRepository.FindByIdAsync(confirmationToken.UserId, ct);

        if (user is null)
            throw new UnauthorizedException(AuthErrorMessages.InvalidOrExpiredToken);

        confirmationToken.MarkAsUsed();
        await _tokenRepository.UpdateAsync(confirmationToken, ct);

        user.ConfirmEmail();
        await _userRepository.UpdateAsync(user, ct);

        return Unit.Value;
    }
}

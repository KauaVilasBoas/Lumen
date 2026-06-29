using FluentValidation;
using Lumen.Modularity;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Configuration;
using Lumen.Modules.Identity.Domain.Security;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Lumen.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lumen.Modules.Identity.Application.Auth.Login;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommandHandler.Command, LoginCommandHandler.Result>
{
    public sealed record Command(string Identifier, string Password, string ClientIp) : IRequest<Result>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Identifier)
                .NotEmpty().WithMessage("O campo identificador é obrigatório.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("O campo senha é obrigatório.");
        }
    }

    public sealed record Result(string AccessToken, string RefreshToken, int ExpiresIn, string TokenType = TokenTypes.Bearer);

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IAppSettings _appSettings;
    private readonly IEventBus _eventBus;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IAppSettings appSettings,
        IEventBus eventBus,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _appSettings = appSettings;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result> Handle(Command cmd, CancellationToken ct)
    {
        var user = await ResolveUserAsync(cmd.Identifier, ct);

        if (user is null)
        {
            _passwordHasher.Verify(cmd.Password, PasswordHashing.DummyBcryptHash);
            _logger.LogWarning("Login failed — identifier not found: {Identifier}", cmd.Identifier);
            throw new UnauthorizedException("Invalid credentials.");
        }

        if (user.IsLockedOut())
        {
            _logger.LogWarning(
                "Login rejected — account {UserId} is locked until {LockedUntil}",
                user.Id, user.LockedUntil);
            throw new AccountLockedException(user.LockedUntil!.Value);
        }

        if (!_passwordHasher.Verify(cmd.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(_appSettings.LockoutThreshold, _appSettings.LockoutDuration);
            await _userRepository.UpdateAsync(user, ct);

            _logger.LogWarning(
                "Login failed — wrong password for user {UserId} (attempts: {Attempts})",
                user.Id, user.FailedLoginAttempts);

            if (user.IsLockedOut())
                await _eventBus.PublishAsync(new UserLockedOutEvent(user.Id, user.Username), ct);

            throw new UnauthorizedException("Invalid credentials.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login rejected — email not confirmed for user {UserId}", user.Id);
            throw new ForbiddenException("Email address not yet confirmed.");
        }

        if (user.FailedLoginAttempts > 0)
            user.Unlock();

        user.RecordLogin();
        await _userRepository.UpdateAsync(user, ct);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshTokenValue();
        var refreshTokenHash = Sha256Hasher.ComputeHex(refreshTokenValue);

        var refreshToken = RefreshToken.Create(
            userId: user.Id,
            tokenHash: refreshTokenHash,
            expiresAt: DateTime.UtcNow.AddDays(_appSettings.RefreshTokenExpirationDays),
            createdByIp: cmd.ClientIp);

        await _refreshTokenRepository.InsertAsync(refreshToken, ct);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        await _eventBus.PublishAsync(new UserLoggedInEvent(user.Id, user.Username), ct);

        return new Result(
            accessToken,
            refreshTokenValue,
            _jwtService.AccessTokenExpiresIn);
    }

    private async Task<User?> ResolveUserAsync(string identifier, CancellationToken ct)
    {
        if (identifier.Contains('@'))
        {
            var normalizedEmail = User.NormalizeEmail(identifier);
            return await _userRepository.FindByEmailAsync(normalizedEmail, ct);
        }

        return await _userRepository.FindByUsernameAsync(identifier, ct);
    }
}

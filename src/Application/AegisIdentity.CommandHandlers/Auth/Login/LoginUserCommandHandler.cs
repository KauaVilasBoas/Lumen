using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using AegisIdentity.SharedKernel.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.CommandHandlers.Auth.Login;

/// <summary>
/// Handles user authentication: validates credentials, enforces lockout, and returns
/// a short-lived JWT access token plus a long-lived opaque refresh token.
/// </summary>
public sealed class LoginUserCommandHandler
    : IRequestHandler<LoginUserCommandHandler.Command, LoginUserCommandHandler.Result>
{
    // ── Nested types ─────────────────────────────────────────────────────────

    /// <summary>
    /// Login input. <see cref="Identifier"/> accepts either an email address or a username;
    /// the presence of '@' is used to discriminate between the two at the handler level.
    /// </summary>
    public sealed record Command(string Identifier, string Password, string ClientIp) : IRequest<Result>;

    /// <summary>Discriminated union result — one subtype per outcome.</summary>
    public abstract class Result
    {
        private Result() { }

        public sealed class Success : Result
        {
            public string AccessToken { get; }
            public string RefreshToken { get; }
            public int ExpiresIn { get; }

            public Success(string accessToken, string refreshToken, int expiresIn)
            {
                AccessToken = accessToken;
                RefreshToken = refreshToken;
                ExpiresIn = expiresIn;
            }
        }

        /// <summary>
        /// Credential is wrong or the user does not exist.
        /// Intentionally opaque — do not reveal whether the identifier was found.
        /// </summary>
        public sealed class InvalidCredentials : Result { }

        /// <summary>The account has not yet confirmed its email address.</summary>
        public sealed class EmailNotConfirmed : Result { }

        /// <summary>The account is temporarily locked due to repeated failed attempts.</summary>
        public sealed class AccountLocked : Result
        {
            public DateTime LockedUntil { get; }

            public AccountLocked(DateTime lockedUntil) => LockedUntil = lockedUntil;
        }
    }

    // ── Dependencies ──────────────────────────────────────────────────────────

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IAppSettings appSettings,
        ILogger<LoginUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _appSettings = appSettings;
        _logger = logger;
    }

    // ── Handler ───────────────────────────────────────────────────────────────

    public async Task<Result> Handle(Command cmd, CancellationToken ct)
    {
        var user = await ResolveUserAsync(cmd.Identifier, ct);

        // Return the same result for "user not found" and "wrong password"
        // to prevent user enumeration via timing differences.
        if (user is null)
        {
            _logger.LogWarning("Login failed — identifier not found: {Identifier}", cmd.Identifier);
            return new Result.InvalidCredentials();
        }

        if (user.IsLockedOut())
        {
            _logger.LogWarning(
                "Login rejected — account {UserId} is locked until {LockedUntil}",
                user.Id, user.LockedUntil);
            return new Result.AccountLocked(user.LockedUntil!.Value);
        }

        if (!_passwordHasher.Verify(cmd.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(_appSettings.LockoutThreshold, _appSettings.LockoutDuration);
            await _userRepository.UpdateAsync(user, ct);

            _logger.LogWarning(
                "Login failed — wrong password for user {UserId} (attempts: {Attempts})",
                user.Id, user.FailedLoginAttempts);

            return new Result.InvalidCredentials();
        }

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Login rejected — email not confirmed for user {UserId}", user.Id);
            return new Result.EmailNotConfirmed();
        }

        // Reset failed attempts on successful authentication.
        if (user.FailedLoginAttempts > 0)
            user.Unlock();

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
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

        return new Result.Success(
            accessToken,
            refreshTokenValue,
            _jwtService.AccessTokenExpiresIn);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a user by email (if the identifier contains '@') or by username.
    /// Email lookup normalises the address before querying.
    /// </summary>
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

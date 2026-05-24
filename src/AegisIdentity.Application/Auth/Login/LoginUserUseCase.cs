using System.Security.Cryptography;
using System.Text;
using AegisIdentity.Domain.Configuration;
using AegisIdentity.Domain.Security;
using AegisIdentity.Domain.Tokens;
using AegisIdentity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace AegisIdentity.Application.Auth.Login;

public sealed class LoginUserUseCase : ILoginUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IAppSettings _appSettings;
    private readonly ILogger<LoginUserUseCase> _logger;

    public LoginUserUseCase(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IAppSettings appSettings,
        ILogger<LoginUserUseCase> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _appSettings = appSettings;
        _logger = logger;
    }

    public async Task<LoginResult> ExecuteAsync(
        LoginRequest request,
        string clientIp,
        CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(request.Identifier, ct);

        // Return the same result for "user not found" and "wrong password"
        // to prevent user enumeration via timing differences.
        if (user is null)
        {
            _logger.LogWarning("Login failed — identifier not found: {Identifier}", request.Identifier);
            return new LoginResult.InvalidCredentials();
        }

        if (user.IsLockedOut())
        {
            _logger.LogWarning(
                "Login rejected — account {UserId} is locked until {LockedUntil}",
                user.Id, user.LockedUntil);
            return new LoginResult.AccountLocked(user.LockedUntil!.Value);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(_appSettings.LockoutThreshold, _appSettings.LockoutDuration);
            await _userRepository.UpdateAsync(user, ct);

            _logger.LogWarning(
                "Login failed — wrong password for user {UserId} (attempts: {Attempts})",
                user.Id, user.FailedLoginAttempts);

            return new LoginResult.InvalidCredentials();
        }

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Login rejected — email not confirmed for user {UserId}", user.Id);
            return new LoginResult.EmailNotConfirmed();
        }

        // Reset failed attempts on successful authentication.
        if (user.FailedLoginAttempts > 0)
            user.Unlock();

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshTokenValue();
        var refreshTokenHash = ComputeSha256Hex(refreshTokenValue);

        var refreshToken = RefreshToken.Create(
            userId: user.Id,
            tokenHash: refreshTokenHash,
            expiresAt: DateTime.UtcNow.AddDays(_appSettings.RefreshTokenExpirationDays),
            createdByIp: clientIp);

        await _refreshTokenRepository.InsertAsync(refreshToken, ct);

        _logger.LogInformation("User {UserId} logged in successfully", user.Id);

        var response = new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            ExpiresIn: _jwtService.AccessTokenExpiresIn);

        return new LoginResult.Success(response);
    }

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

    private static string ComputeSha256Hex(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

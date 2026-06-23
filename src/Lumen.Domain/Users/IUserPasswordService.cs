namespace Lumen.Domain.Users;

public interface IUserPasswordService
{
    Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default);

    Task SendPasswordChangedEmailAsync(User user, CancellationToken ct = default);
}

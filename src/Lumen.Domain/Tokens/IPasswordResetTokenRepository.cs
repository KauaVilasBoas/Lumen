namespace Lumen.Domain.Tokens;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task InsertAsync(PasswordResetToken token, CancellationToken ct = default);

    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
}

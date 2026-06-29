namespace Lumen.Modules.Identity.Domain.Tokens;

internal interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task InsertAsync(PasswordResetToken token, CancellationToken ct = default);

    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
}

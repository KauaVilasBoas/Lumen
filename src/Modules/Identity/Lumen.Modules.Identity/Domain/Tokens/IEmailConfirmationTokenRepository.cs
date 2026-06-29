namespace Lumen.Modules.Identity.Domain.Tokens;

internal interface IEmailConfirmationTokenRepository
{
    Task<EmailConfirmationToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task InsertAsync(EmailConfirmationToken token, CancellationToken ct = default);

    Task UpdateAsync(EmailConfirmationToken token, CancellationToken ct = default);

    Task InvalidateByUserIdAsync(Guid userId, CancellationToken ct = default);
}

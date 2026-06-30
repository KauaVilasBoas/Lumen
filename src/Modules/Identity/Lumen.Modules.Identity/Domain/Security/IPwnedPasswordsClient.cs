namespace Lumen.Modules.Identity.Domain.Security;

internal interface IPwnedPasswordsClient
{
    Task<bool> IsPwnedAsync(string password, CancellationToken ct = default);
}

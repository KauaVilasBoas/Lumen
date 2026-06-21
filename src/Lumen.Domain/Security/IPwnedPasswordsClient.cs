namespace AegisIdentity.Domain.Security;

public interface IPwnedPasswordsClient
{
    Task<bool> IsPwnedAsync(string password, CancellationToken ct = default);
}

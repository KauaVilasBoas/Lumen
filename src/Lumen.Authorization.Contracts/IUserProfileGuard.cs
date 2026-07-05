namespace Lumen.Authorization.Contracts;

public interface IUserProfileGuard
{
    Task<bool> IsUserAdministratorAsync(Guid userId, CancellationToken ct = default);

    Task<int> CountActiveAdministratorsAsync(Guid administratorProfileId, CancellationToken ct = default);
}

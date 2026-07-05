using Lumen.Authorization.Contracts;
using Lumen.Authorization.Domain;

namespace Lumen.Authorization.Application;

internal sealed class UserProfileGuard : IUserProfileGuard
{
    private readonly IUserProfileRepository _userProfileRepository;

    public UserProfileGuard(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<bool> IsUserAdministratorAsync(Guid userId, CancellationToken ct = default)
        => await _userProfileRepository.FindActiveAsync(userId, SystemProfiles.AdministratorId, ct) is not null;

    public async Task<int> CountActiveAdministratorsAsync(Guid administratorProfileId, CancellationToken ct = default)
    {
        var admins = await _userProfileRepository.ListByProfileIdAsync(administratorProfileId, ct);
        return admins.Count;
    }
}

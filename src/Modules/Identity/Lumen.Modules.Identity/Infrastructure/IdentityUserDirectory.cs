using Lumen.Authorization.Contracts;
using Lumen.Modules.Identity.Domain.Users;

namespace Lumen.Modules.Identity.Infrastructure;

internal sealed class IdentityUserDirectory : IUserDirectory
{
    private readonly IUserRepository _userRepository;

    public IdentityUserDirectory(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, ct);
        return user?.Username;
    }
}

using Lumen.Authorization.Contracts;
using Lumen.Identity.Domain.Users;

namespace Lumen.Identity.Infrastructure.Bridges;

/// <summary>
/// Implements <see cref="IUserDirectory"/> by resolving display names from the Identity
/// user store. Used by Authorization audit/logging without coupling to Identity internals.
/// </summary>
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

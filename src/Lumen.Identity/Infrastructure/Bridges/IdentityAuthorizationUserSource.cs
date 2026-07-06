using Lumen.Authorization.Contracts;
using Lumen.Identity.Application.Users;
using Lumen.Identity.Application.Users.List;
using Lumen.Identity.Domain.Users;

namespace Lumen.Identity.Infrastructure.Bridges;

/// <summary>
/// Implements <see cref="IAuthorizationUserSource"/> by reading users from the Identity
/// user store, so that the Authorization module can list active users without a direct
/// dependency on the Identity domain.
/// </summary>
internal sealed class IdentityAuthorizationUserSource : IAuthorizationUserSource
{
    private readonly IUserRepository _userRepository;

    public IdentityAuthorizationUserSource(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<AuthorizationUserDto>> ListActiveUsersAsync(CancellationToken ct = default)
    {
        var (users, _) = await _userRepository.ListAsync(
            search: null,
            includeDeleted: false,
            page: 1,
            pageSize: int.MaxValue,
            ct);

        var now = DateTime.UtcNow;

        return users
            .Select(u => new AuthorizationUserDto(
                Id: u.Id,
                Username: u.Username,
                Email: u.Email,
                State: UserStateResolver.Resolve(u, now)))
            .ToList();
    }
}

using Lumen.Authorization.Contracts;
using Lumen.Modules.Identity.Application.Queries;
using Lumen.Modules.Identity.Domain.Users;

namespace Lumen.Modules.Identity.Infrastructure;

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

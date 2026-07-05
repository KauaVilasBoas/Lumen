using Lumen.Authorization.Contracts;

namespace Lumen.Authorization.Infrastructure;

internal sealed class EmptyAuthorizationUserSource : IAuthorizationUserSource
{
    public Task<IReadOnlyList<AuthorizationUserDto>> ListActiveUsersAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AuthorizationUserDto>>([]);
}

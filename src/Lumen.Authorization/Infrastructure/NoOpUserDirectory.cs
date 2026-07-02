using Lumen.Authorization.Contracts;

namespace Lumen.Authorization.Infrastructure;

internal sealed class NoOpUserDirectory : IUserDirectory
{
    public Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}

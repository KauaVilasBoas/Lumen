namespace Lumen.Authorization.Contracts;

public interface IUserDirectory
{
    Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken ct = default);
}

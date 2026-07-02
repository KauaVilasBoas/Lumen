namespace Lumen.Authorization.Contracts;

public interface IAuthorizationUserSource
{
    Task<IReadOnlyList<AuthorizationUserDto>> ListActiveUsersAsync(CancellationToken ct = default);
}

public sealed record AuthorizationUserDto(
    Guid Id,
    string Username,
    string Email,
    string State);

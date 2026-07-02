using System.Security.Claims;

namespace Lumen.Authorization.Contracts;

public interface IUserIdAccessor
{
    bool TryGetUserId(ClaimsPrincipal principal, out Guid userId);
}

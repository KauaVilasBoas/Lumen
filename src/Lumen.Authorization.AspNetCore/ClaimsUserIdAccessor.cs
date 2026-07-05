using System.Security.Claims;
using Lumen.Authorization.Contracts;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.AspNetCore;

public sealed class ClaimsUserIdAccessor : IUserIdAccessor
{
    private readonly string _claimType;

    public ClaimsUserIdAccessor(IOptions<LumenAuthorizationOptions> options)
    {
        _claimType = options.Value.UserIdClaimType;
    }

    public bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(_claimType);
        return Guid.TryParse(value, out userId);
    }
}

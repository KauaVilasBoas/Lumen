using Microsoft.AspNetCore.Authorization;

namespace Lumen.Authorization.AspNetCore;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string? Code { get; }

    public PermissionRequirement(string? code = null)
    {
        Code = code;
    }
}

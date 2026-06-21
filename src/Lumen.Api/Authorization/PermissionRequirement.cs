using Microsoft.AspNetCore.Authorization;

namespace AegisIdentity.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Code { get; }

    public PermissionRequirement(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }
}

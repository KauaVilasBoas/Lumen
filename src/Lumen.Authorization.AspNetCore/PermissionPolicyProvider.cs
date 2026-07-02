using Lumen.Authorization.AspNetCore.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.AspNetCore;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await _fallback.GetPolicyAsync(policyName);

        if (existing is not null)
            return existing;

        var code = ResolvePermissionCode(policyName);

        if (code is null)
            return null;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(code))
            .Build();
    }

    private static string? ResolvePermissionCode(string policyName)
    {
        if (policyName.StartsWith(AuthorizationPolicyPrefixes.Lumen, StringComparison.Ordinal))
            return policyName[AuthorizationPolicyPrefixes.Lumen.Length..];

        if (policyName.Contains('.', StringComparison.Ordinal))
            return policyName;

        return null;
    }
}

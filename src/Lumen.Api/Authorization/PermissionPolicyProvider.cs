using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AegisIdentity.Api.Authorization;

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

        if (!LooksLikePermissionCode(policyName))
            return null;

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }

    private static bool LooksLikePermissionCode(string name)
        => name.Contains('.', StringComparison.Ordinal);
}

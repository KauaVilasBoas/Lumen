using Microsoft.AspNetCore.Authorization;

namespace Lumen.Authorization.AspNetCore;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute, IAuthorizationRequirementData, IAuthorizeData
{
    public string? Code { get; }

    public string? Policy { get; set; }
    public string? Roles { get; set; }
    public string? AuthenticationSchemes { get; set; }

    public RequirePermissionAttribute() { }

    public RequirePermissionAttribute(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermissionRequirement(Code);
    }
}

namespace Lumen.SharedKernel.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequirePermissionAttribute : Attribute
{
    public string? Code { get; }

    public RequirePermissionAttribute() { }

    public RequirePermissionAttribute(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        Code = code;
    }
}

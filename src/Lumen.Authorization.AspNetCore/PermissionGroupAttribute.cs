namespace Lumen.Authorization.AspNetCore;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class PermissionGroupAttribute : Attribute
{
    public string Name { get; }

    public PermissionGroupAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}

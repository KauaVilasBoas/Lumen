using Lumen.Authorization;

namespace Lumen.Identity;

/// <summary>
/// Options for Lumen.Identity core registration via <c>AddLumenIdentityCore()</c>.
/// </summary>
public sealed class LumenIdentityOptions
{
    /// <summary>
    /// Database provider. Defaults to <see cref="DatabaseProvider.SqlServer"/>.
    /// Must match the provider used by <c>Lumen.Authorization</c> in the same application.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
}

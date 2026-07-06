using Lumen.Authorization.Contracts;

namespace Lumen.Authorization.Infrastructure;

/// <summary>
/// Default <see cref="ITenantScopeAccessor"/> that always returns <c>null</c>,
/// preserving the global authorization behavior for applications that do not use tenants.
/// </summary>
/// <remarks>
/// Registered via <c>TryAddScoped</c> so that a host can override it with its own
/// implementation without any conflict.
/// </remarks>
internal sealed class NoOpTenantScopeAccessor : ITenantScopeAccessor
{
    public Guid? GetCurrentScopeId() => null;
}

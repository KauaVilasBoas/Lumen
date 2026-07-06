using Lumen.Modularity;

namespace Lumen.Authorization.Contracts.Events;

/// <param name="UserId">The user whose permissions changed.</param>
/// <param name="ScopeId">
/// The scope in which permissions changed, or <c>null</c> for global assignments.
/// Cache invalidation targets the specific <c>(UserId, ScopeId)</c> entry.
/// </param>
public sealed record UserPermissionsChangedEvent(Guid UserId, Guid? ScopeId = null) : IntegrationEvent;

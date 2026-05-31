namespace AegisIdentity.Api.Authorization;

public sealed record DiscoveredPermission(
    string Controller,
    string Action,
    string Code,
    string DisplayName,
    string GroupName);

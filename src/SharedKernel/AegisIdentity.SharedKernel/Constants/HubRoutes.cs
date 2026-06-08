namespace AegisIdentity.SharedKernel.Constants;

public static class HubRoutes
{
    public const string AuthorizationGraph = "/hubs/authorization-graph";
}

public static class HubMethods
{
    public static class AuthorizationGraph
    {
        public const string GraphUpdated = "GraphUpdated";
        public const string UserPermissionsInvalidated = "UserPermissionsInvalidated";
    }
}

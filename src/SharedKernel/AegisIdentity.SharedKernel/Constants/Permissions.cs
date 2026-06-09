namespace AegisIdentity.SharedKernel.Constants;

public static class PermissionCodes
{
    public static class AuthorizationGraph
    {
        public const string View = "AuthorizationGraph.View";
    }

    public static class Users
    {
        public const string List = "Users.List";
        public const string Get  = "Users.Get";
    }

    public static class Audit
    {
        public const string Read = "Audit.Read";
    }

    public static class Diagnostics
    {
        public const string GetCacheStats = "Diagnostics.GetCacheStats";
        public const string GetJobStats   = "Diagnostics.GetJobStats";
    }
}

public static class PermissionGroups
{
    public const string Authorization = "Authorization";
    public const string Users         = "Users";
    public const string Audit         = "Audit";
    public const string Diagnostics   = "Diagnostics";
}

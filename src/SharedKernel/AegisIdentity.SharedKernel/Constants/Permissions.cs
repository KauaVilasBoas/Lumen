namespace AegisIdentity.SharedKernel.Constants;

public static class PermissionCodes
{
    public static class AuthorizationGraph
    {
        public const string View = "AuthorizationGraph.View";
    }

    public static class Users
    {
        public const string List    = "Users.List";
        public const string Get     = "Users.Get";
        public const string Update  = "Users.Update";
        public const string Delete  = "Users.Delete";
        public const string Restore = "Users.Restore";
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

    public static class Profiles
    {
        public const string List           = "Profiles.List";
        public const string Get            = "Profiles.Get";
        public const string Create         = "Profiles.Create";
        public const string Update         = "Profiles.Update";
        public const string Delete         = "Profiles.Delete";
        public const string SetPermissions = "Profiles.SetPermissions";
    }

    public static class Permissions
    {
        public const string List = "Permissions.List";
    }

    public static class UserProfiles
    {
        public const string List   = "UserProfiles.List";
        public const string Assign = "UserProfiles.Assign";
        public const string Remove = "UserProfiles.Remove";
    }
}

public static class PermissionGroups
{
    public const string Authorization = "Authorization";
    public const string Users         = "Users";
    public const string Audit         = "Audit";
    public const string Diagnostics   = "Diagnostics";
    public const string Profiles      = "Profiles";
    public const string Permissions   = "Permissions";
    public const string UserProfiles  = "UserProfiles";
}

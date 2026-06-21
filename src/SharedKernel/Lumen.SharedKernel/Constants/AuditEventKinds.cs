namespace Lumen.SharedKernel.Constants;

public static class AuditEventKinds
{
    public const string AuthLogin        = "auth.login";
    public const string AuthLockout      = "auth.lockout";
    public const string CacheInvalidate  = "cache.invalidate";
    public const string ProfilePermSet   = "profile.permset";
    public const string UserProfileAssign  = "userprofile.assign";
    public const string UserProfileRemove  = "userprofile.remove";
    public const string JobCleanup       = "job.cleanup";
    public const string UserUpdated      = "user.updated";
}

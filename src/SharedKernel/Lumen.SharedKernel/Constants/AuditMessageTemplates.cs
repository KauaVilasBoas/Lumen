namespace Lumen.SharedKernel.Constants;

public static class AuditMessageTemplates
{
    public const string UserLoggedIn                = "User '{0}' logged in.";
    public const string UserLockedOut               = "Account '{0}' locked out after repeated failed login attempts.";
    public const string ProfilePermissionsUpdated   = "Permissions updated on profile '{0}' by '{1}'.";
    public const string UserProfileAssigned         = "Profile '{0}' assigned to user '{1}'.";
    public const string UserProfileRemoved          = "Profile '{0}' removed from user '{1}'.";
    public const string UserPermissionCacheInvalidated = "Permission cache invalidated for user '{0}'.";
    public const string CleanupJobExecuted          = "Job '{0}' executed — {1} record(s) deleted.";
    public const string UserSoftDeleted             = "User '{0}' ({1}) soft-deleted.";
    public const string UserRestored                = "User '{0}' ({1}) restored from soft delete.";
}

namespace AegisIdentity.SharedKernel.Constants;

public static class ProfileErrorMessages
{
    public const string ProfileIdRequired          = "ProfileId is required.";
    public const string ProfileNameRequired        = "Profile name is required.";
    public const string ProfileNameTooLong         = "Profile name must not exceed 128 characters.";
    public const string ProfileDescriptionRequired = "Profile description is required.";
    public const string ProfileDescriptionTooLong  = "Profile description must not exceed 512 characters.";

    public const string UserIdRequired = "UserId is required.";

    public const string PermissionIdsRequired = "PermissionIds is required.";
    public const string PermissionIdInvalid   = "Each PermissionId must be a valid non-empty Guid.";

    public const string UserNotFoundForProfile    = "User '{0}' not found.";
    public const string ProfileNotFound           = "Profile '{0}' not found.";
    public const string PermissionNotFound        = "Permission '{0}' not found.";
    public const string ProfileNameConflict       = "A profile with name '{0}' already exists.";
    public const string SystemProfileCannotDelete = "System profile '{0}' cannot be deleted.";
    public const string SystemProfileCannotRename = "System profile '{0}' cannot be renamed.";
    public const string SystemProfilePermissionsReadOnly = "Permissions on system profile '{0}' are managed automatically and cannot be overwritten via the API.";
    public const string ActiveAssignmentNotFound  = "Active assignment of user '{0}' to profile '{1}' not found.";

    public const string UserNotFoundInDetail = "User '{0}' was not found.";
}

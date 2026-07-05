namespace Lumen.Authorization.Backoffice.Internal;

internal static class BackofficeErrorMessages
{
    internal const string CreateProfileError = "Failed to create profile. Please try again.";
    internal const string UpdateProfileError = "Failed to update profile. Please try again.";
    internal const string DeleteProfileError = "Failed to delete profile. Please try again.";
    internal const string SetPermissionsError = "Failed to update permissions. Please try again.";
    internal const string ProfileNotFound = "Profile not found.";
    internal const string ProfileIsSystem = "System profiles cannot be modified.";
    internal const string AssignProfileError = "Failed to assign profile to user. Please try again.";
    internal const string RemoveProfileError = "Failed to remove profile from user. Please try again.";
    internal const string UserSourceNotConfigured =
        "No active users available. Implement IAuthorizationUserSource and register it " +
        "with services.AddSingleton<IAuthorizationUserSource, YourImplementation>() " +
        "to populate this list.";
}

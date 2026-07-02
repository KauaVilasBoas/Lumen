namespace Lumen.Authorization.Backoffice.Internal;

internal static class BackofficeDisplayTokens
{
    internal const string ProfileAccentAdministrator = "#8b6dff";
    internal const string ProfileAccentSystemDefault = "#5b6478";

    internal static readonly string[] ProfileAccentPalette =
        ["#4c8dff", "#2bd4a0", "#f5a623", "#f25fa6", "#a78bfa"];

    internal static string ProfileAccent(string name, bool isSystem)
    {
        if (!isSystem)
            return ProfileAccentPalette[Math.Abs(name.GetHashCode()) % ProfileAccentPalette.Length];

        return name == "Administrator" ? ProfileAccentAdministrator : ProfileAccentSystemDefault;
    }

    internal const string PageTitleProfiles = "Profiles";
    internal const string PageSubtitleProfiles = "Role-like containers · permission assignment";
    internal const string PageTitlePermissions = "Permissions";
    internal const string PageSubtitlePermissions = "Discovered endpoint actions · Controller.Action";
    internal const string PageTitleCreate = "New profile";
    internal const string PageSubtitleCreate = "Create a role-like container";
    internal const string PageTitleEdit = "Edit profile";
    internal const string PageSubtitleEdit = "Update a role-like container";
    internal const string PageSubtitleDetails = "Permission assignment matrix";
}

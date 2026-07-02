namespace Lumen.Authorization.Backoffice.Internal;

internal static class PermissionDisplayHelper
{
    internal static string HttpMethod(string permissionCode)
    {
        var action = permissionCode.Contains('.') ? permissionCode.Split('.')[1] : permissionCode;
        if (action is "Create" or "Assign" or "Register" or "Login") return "POST";
        if (action is "Update" or "SetPermissions") return "PUT";
        if (action is "Delete" or "Remove") return "DELETE";
        return "GET";
    }

    internal static string MethodCssColor(string httpMethod) => httpMethod switch
    {
        "POST"   => "var(--lumen-pres)",
        "PUT"    => "var(--lumen-warn)",
        "DELETE" => "var(--lumen-danger)",
        _        => "var(--lumen-app)"
    };
}

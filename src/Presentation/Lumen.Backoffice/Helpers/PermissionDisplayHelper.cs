namespace AegisIdentity.Backoffice.Helpers;

public static class PermissionDisplayHelper
{
    public static string HttpMethod(string permissionCode)
    {
        var action = permissionCode.Contains('.') ? permissionCode.Split('.')[1] : permissionCode;
        if (action is "Create" or "Assign" or "Register" or "Login") return "POST";
        if (action is "Update" or "SetPermissions") return "PUT";
        if (action is "Delete" or "Remove") return "DELETE";
        return "GET";
    }

    public static string MethodCssColor(string httpMethod) => httpMethod switch
    {
        "POST"   => "var(--pres)",
        "PUT"    => "var(--warn)",
        "DELETE" => "var(--danger)",
        _        => "var(--app)"
    };
}

namespace Lumen.Authorization.AspNetCore;

public static class ControllerNameNormalizer
{
    private const string ControllerSuffix = "Controller";

    public static string Normalize(string controllerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controllerName);

        if (controllerName.EndsWith(ControllerSuffix, StringComparison.Ordinal)
            && controllerName.Length > ControllerSuffix.Length)
        {
            return controllerName[..^ControllerSuffix.Length];
        }

        return controllerName;
    }
}

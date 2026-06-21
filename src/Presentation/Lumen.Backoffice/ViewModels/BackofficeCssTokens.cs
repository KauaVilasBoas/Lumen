namespace Lumen.Backoffice.ViewModels;

public static class BackofficeCssTokens
{
    public static readonly string[] AvatarGradients =
    [
        "linear-gradient(135deg,#9a7dff,#6b49f0)",
        "linear-gradient(135deg,#4c8dff,#2a5fd6)",
        "linear-gradient(135deg,#2bd4a0,#159e78)",
        "linear-gradient(135deg,#f5a623,#d4830a)",
        "linear-gradient(135deg,#f25fa6,#c43e87)",
        "linear-gradient(135deg,#a78bfa,#7c59e0)",
        "linear-gradient(135deg,#34d399,#059669)",
    ];

    public static readonly string[] ProfileAccentColors =
        ["#4c8dff", "#2bd4a0", "#f5a623", "#f25fa6", "#a78bfa"];

    public const string ProfileAccentAdministrator = "#8b6dff";
    public const string ProfileAccentSystemDefault = "#5b6478";

    public const string LifecycleColorPresentation = "var(--pres)";
    public const string LifecycleColorApplication  = "var(--app)";
    public const string LifecycleColorDomain        = "var(--dom)";
    public const string LifecycleColorDanger        = "var(--danger)";
    public const string LifecycleColorFaint         = "var(--text-faint)";
    public const string LifecycleColorWarning       = "var(--warn)";
    public const string LifecycleColorOk            = "var(--ok)";
}

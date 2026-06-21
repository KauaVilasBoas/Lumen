namespace Lumen.Backoffice.ViewModels;

public static class BackofficeDisplayLabels
{
    public const string Registered            = "Registered";
    public const string EmailConfirmed        = "Email confirmed";
    public const string LastLogin             = "Last login";
    public const string LockedOut             = "Locked out";
    public const string SoftDeleted           = "Soft-deleted";
    public const string AwaitingConfirmation  = "Awaiting confirmation";
    public const string ActiveSession         = "Active session";

    public const string EmailPending          = "pending";
    public const string NoLoginDate           = "—";
    public const string LockoutIndefinite     = "indefinite";
    public const string SoftDeletedDate       = "account removed";
    public const string AwaitingBlocked       = "blocked";
    public const string ActiveSessionDate     = "JWT valid";

    public const string RegisteredNote        = "isActive = false until confirmed";
    public const string LockoutNote           = "423 until lockout expires";
    public const string SoftDeletedNote       = "row retained · email re-registerable";
    public const string AwaitingNote          = "403 until email confirmed";
}

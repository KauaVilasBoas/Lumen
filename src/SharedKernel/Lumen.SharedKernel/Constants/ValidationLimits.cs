namespace AegisIdentity.SharedKernel.Constants;

public static class ValidationLimits
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;
    public const int EmailMaxLength = 256;
    public const int PasswordMinLength = 12;
    public const string PasswordSpecialCharacters = "!@#$%^&*()-_=+[]{};:'\",.<>/?\\|`~";

    public const int PageMinValue = 1;
    public const int PageSizeMinValue = 1;
    public const int PageSizeMaxValue = 100;
    public const int AuditTakeMinValue = 1;
    public const int AuditTakeMaxValue = 100;
    public const int AuditTakeDefaultValue = 20;

    public const int UserRestoreWindowDays = 30;

    public const string UsernameAllowedCharsPattern = "^[a-zA-Z0-9_-]+$";
}

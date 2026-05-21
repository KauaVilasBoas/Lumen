namespace AegisIdentity.Infrastructure.Notifications;

// Strongly-typed catalogue of available email templates. Adding a new template means:
// 1. drop the .html and .txt under Templates/Email/,
// 2. add an entry here. The renderer resolves the embedded resources by template name.
public enum EmailTemplate
{
    EmailConfirmation,
    PasswordReset,
    PasswordChanged,
}

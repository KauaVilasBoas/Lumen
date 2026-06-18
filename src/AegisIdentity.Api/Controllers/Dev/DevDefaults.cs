namespace AegisIdentity.Api.Controllers.Dev;

internal static class DevDefaults
{
    public const string TestEmailRecipientDisplayName = "Developer";
    public const string TestEmailSubject              = "AegisIdentity Mailpit Smoke Test";
    public const string TestEmailConfirmationUrl      = "http://localhost:5237/dev/email-test";
    public const string MailpitViewerUrl              = "http://localhost:8025";
    public const string ToQueryParamRequired          = "Query parameter 'to' is required.";
}

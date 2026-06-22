using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Options;

namespace Lumen.Infrastructure.Configuration;

/// <summary>
/// Production-only validation for <see cref="SmtpOptions"/>, registered by
/// <see cref="InfrastructureOptionsExtensions.AddInfrastructureOptions"/> and executed
/// on startup via <c>ValidateOnStart</c> — a misconfigured SMTP relay fails the boot
/// instead of silently discarding outbound emails.
/// <para>
/// Complements the data-annotation rules on <see cref="SmtpOptions"/> with checks that
/// only make sense in Production: committed <c>REPLACE_ME</c> placeholders, loopback
/// hosts and the anonymous relay credentials that are valid for dev Mailpit.
/// Failure messages name the offending environment variable but never echo its value.
/// </para>
/// </summary>
public sealed class SmtpProductionOptionsValidator : IValidateOptions<SmtpOptions>
{
    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        var failures = new List<string>();

        ValidateHost(options.Host, failures);
        ValidateRequired(options.User, "Smtp__User", failures);
        ValidateRequired(options.Pass, "Smtp__Pass", failures);
        ValidateRequired(options.From, "Smtp__From", failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateHost(string host, List<string> failures)
    {
        if (IsUnset(host))
        {
            failures.Add(UnsetMessage("Smtp__Host"));
            return;
        }

        if (ConfigurationPlaceholders.LoopbackHostAliases.Contains(host, StringComparer.OrdinalIgnoreCase))
            failures.Add(
                "Smtp__Host resolves to localhost. A loopback SMTP relay in Production would " +
                "silently discard all outbound emails. Set Smtp__Host to a real SMTP server " +
                "(e.g. smtp-relay.brevo.com).");
    }

    private static void ValidateRequired(string value, string variableName, List<string> failures)
    {
        if (IsUnset(value))
            failures.Add(UnsetMessage(variableName));
    }

    private static bool IsUnset(string value)
        => string.IsNullOrWhiteSpace(value)
           || string.Equals(value, ConfigurationPlaceholders.ReplaceMe, StringComparison.OrdinalIgnoreCase);

    private static string UnsetMessage(string variableName)
        => $"{variableName} is required in Production and is missing or still set to the " +
           $"'{ConfigurationPlaceholders.ReplaceMe}' placeholder. See docs/configuration.md.";
}

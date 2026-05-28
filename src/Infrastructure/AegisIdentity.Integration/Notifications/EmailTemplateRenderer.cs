using System.Collections.Concurrent;
using System.Reflection;

namespace AegisIdentity.Integration.Notifications;

// Renders email bodies by loading embedded .html/.txt templates and replacing
// `{{Placeholder}}` tokens with caller-supplied values. Razor was rejected for the
// MVP: it would either drag ASP.NET into Infrastructure or push template
// rendering into the API layer, both for three short transactional emails.
public sealed class EmailTemplateRenderer
{
    private static readonly Assembly TemplateAssembly = typeof(EmailTemplateRenderer).Assembly;
    private const string ResourceNamespace = "AegisIdentity.Integration.Templates.Email";

    // Embedded resources never change between calls; cache the raw content per resource name.
    private static readonly ConcurrentDictionary<string, string> ContentCache = new();

    public (string HtmlBody, string TextBody) Render(
        EmailTemplate template,
        IReadOnlyDictionary<string, string> placeholders)
    {
        ArgumentNullException.ThrowIfNull(placeholders);

        var html = LoadAndReplace($"{template}.html", placeholders);
        var text = LoadAndReplace($"{template}.txt", placeholders);
        return (html, text);
    }

    private static string LoadAndReplace(string fileName, IReadOnlyDictionary<string, string> placeholders)
    {
        var raw = ContentCache.GetOrAdd(fileName, LoadEmbeddedResource);
        return ApplyPlaceholders(raw, placeholders);
    }

    private static string LoadEmbeddedResource(string fileName)
    {
        var resourceName = $"{ResourceNamespace}.{fileName}";
        using var stream = TemplateAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Email template '{resourceName}' was not found as an embedded resource. " +
                "Verify the file exists under Templates/Email/ and is included as <EmbeddedResource>.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ApplyPlaceholders(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        if (placeholders.Count == 0)
            return template;

        var result = template;
        foreach (var (key, value) in placeholders)
            result = result.Replace("{{" + key + "}}", value ?? string.Empty, StringComparison.Ordinal);

        return result;
    }
}

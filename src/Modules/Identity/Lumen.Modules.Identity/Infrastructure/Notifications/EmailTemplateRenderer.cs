using System.Collections.Concurrent;
using System.Reflection;
using Lumen.Modules.Identity.Domain.Notifications;

namespace Lumen.Modules.Identity.Infrastructure.Notifications;

internal sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly TemplateAssembly = typeof(EmailTemplateRenderer).Assembly;
    private const string ResourceNamespace = "Lumen.Modules.Identity.Infrastructure.Notifications.Templates.Email";

    private static readonly ConcurrentDictionary<string, string> ContentCache = new();

    public (string HtmlBody, string TextBody) Render(string templateName, IReadOnlyDictionary<string, string> placeholders)
    {
        ArgumentNullException.ThrowIfNull(placeholders);

        var html = LoadAndReplace($"{templateName}.html", placeholders);
        var text = LoadAndReplace($"{templateName}.txt", placeholders);
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
                $"Email template '{resourceName}' was not found as an embedded resource.");
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

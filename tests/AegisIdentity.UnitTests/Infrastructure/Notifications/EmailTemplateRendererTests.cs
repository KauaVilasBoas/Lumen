using AegisIdentity.Infrastructure.Notifications;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Infrastructure.Notifications;

public sealed class EmailTemplateRendererTests
{
    private readonly EmailTemplateRenderer _renderer = new();

    [Fact]
    public void Render_EmailConfirmation_SubstitutesPlaceholdersInBothBodies()
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = "Alice",
            ["ConfirmationUrl"] = "https://app/confirm?token=abc",
        };

        var (html, text) = _renderer.Render(EmailTemplate.EmailConfirmation, placeholders);

        html.Should().Contain("Alice").And.Contain("https://app/confirm?token=abc");
        html.Should().NotContain("{{UserName}}").And.NotContain("{{ConfirmationUrl}}");

        text.Should().Contain("Alice").And.Contain("https://app/confirm?token=abc");
        text.Should().NotContain("{{UserName}}").And.NotContain("{{ConfirmationUrl}}");
    }

    [Fact]
    public void Render_PasswordReset_SubstitutesResetUrlPlaceholder()
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = "Bob",
            ["ResetUrl"] = "https://app/reset?token=xyz",
        };

        var (html, text) = _renderer.Render(EmailTemplate.PasswordReset, placeholders);

        html.Should().Contain("https://app/reset?token=xyz");
        text.Should().Contain("https://app/reset?token=xyz");
    }

    [Fact]
    public void Render_PasswordChanged_SubstitutesChangedAtPlaceholder()
    {
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = "Carol",
            ["ChangedAt"] = "2026-05-21 10:30 UTC",
        };

        var (html, text) = _renderer.Render(EmailTemplate.PasswordChanged, placeholders);

        html.Should().Contain("2026-05-21 10:30 UTC");
        text.Should().Contain("2026-05-21 10:30 UTC");
    }

    [Fact]
    public void Render_WithNullPlaceholderValue_TreatsItAsEmptyString()
    {
        // A defensive substitution should not blow up if a caller passes null.
        var placeholders = new Dictionary<string, string>
        {
            ["UserName"] = null!,
            ["ConfirmationUrl"] = "x",
        };

        var act = () => _renderer.Render(EmailTemplate.EmailConfirmation, placeholders);

        act.Should().NotThrow();
    }

    [Fact]
    public void Render_WithNullPlaceholders_Throws()
    {
        var act = () => _renderer.Render(EmailTemplate.EmailConfirmation, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Render_LeavesUnknownPlaceholdersUntouched()
    {
        // Unknown keys in the dictionary are inert: they should not affect output.
        var (html, _) = _renderer.Render(
            EmailTemplate.EmailConfirmation,
            new Dictionary<string, string>
            {
                ["UserName"] = "real-user",
                ["ConfirmationUrl"] = "https://app/confirm",
                ["UnusedKey"] = "should-not-appear",
            });

        html.Should().Contain("real-user").And.Contain("https://app/confirm");
        html.Should().NotContain("should-not-appear");
    }

    [Fact]
    public void Render_LeavesUnsubstitutedTemplatePlaceholdersAsIs()
    {
        // If a caller forgets a key, the literal "{{Name}}" stays in the output.
        // Better than silently shipping an empty value.
        var (html, _) = _renderer.Render(
            EmailTemplate.EmailConfirmation,
            new Dictionary<string, string> { ["UserName"] = "Dave" });

        html.Should().Contain("Dave");
        html.Should().Contain("{{ConfirmationUrl}}");
    }
}

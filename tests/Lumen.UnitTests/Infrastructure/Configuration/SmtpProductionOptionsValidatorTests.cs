using Lumen.Infrastructure.Configuration;
using FluentAssertions;

namespace Lumen.UnitTests.Infrastructure.Configuration;

public sealed class SmtpProductionOptionsValidatorTests
{
    private static readonly SmtpProductionOptionsValidator Validator = new();

    private static SmtpOptions ValidOptions(
        string host = "smtp-relay.brevo.com",
        string user = "apikey",
        string pass = "super-secret-value",
        string from = "no-reply@aegisidentity.io")
        => new() { Host = host, Port = 587, User = user, Pass = pass, From = from };

    [Fact]
    public void Validate_WithFullyConfiguredOptions_Succeeds()
    {
        var result = Validator.Validate(null, ValidOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("REPLACE_ME")]
    [InlineData("replace_me")]
    [InlineData("Replace_Me")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenHostIsUnset_FailsNamingTheEnvironmentVariable(string host)
    {
        var result = Validator.Validate(null, ValidOptions(host: host));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Smtp__Host");
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public void Validate_WhenHostIsLoopback_Fails(string host)
    {
        var result = Validator.Validate(null, ValidOptions(host: host));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("localhost");
    }

    [Theory]
    [InlineData("REPLACE_ME")]
    [InlineData("")]
    public void Validate_WhenUserIsUnset_FailsNamingTheEnvironmentVariable(string user)
    {
        var result = Validator.Validate(null, ValidOptions(user: user));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Smtp__User");
    }

    [Theory]
    [InlineData("REPLACE_ME")]
    [InlineData("")]
    public void Validate_WhenPassIsUnset_FailsNamingTheEnvironmentVariable(string pass)
    {
        var result = Validator.Validate(null, ValidOptions(pass: pass));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Smtp__Pass");
    }

    [Theory]
    [InlineData("REPLACE_ME")]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenFromIsUnset_FailsNamingTheEnvironmentVariable(string from)
    {
        var result = Validator.Validate(null, ValidOptions(from: from));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Smtp__From");
    }

    [Fact]
    public void Validate_WithMultipleProblems_ReportsAllOfThemAtOnce()
    {
        var options = new SmtpOptions
        {
            Host = "localhost",
            User = "REPLACE_ME",
            Pass = "",
            From = "REPLACE_ME",
        };

        var result = Validator.Validate(null, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should()
            .Contain("localhost").And
            .Contain("Smtp__User").And
            .Contain("Smtp__Pass").And
            .Contain("Smtp__From");
    }

    [Fact]
    public void Validate_FailureMessages_NeverEchoConfiguredSecrets()
    {
        var leakedSecret = "hunter2-production-password";
        var result = Validator.Validate(null, ValidOptions(host: "localhost", pass: leakedSecret));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().NotContain(leakedSecret);
    }
}

using Lumen.Authorization.AspNetCore;
using FluentAssertions;

namespace Lumen.UnitTests.Authorization;

public sealed class ConventionPermissionRegressionTests
{
    [Theory]
    [InlineData("UsersController", "Users")]
    [InlineData("AuditController", "Audit")]
    [InlineData("DiagnosticsController", "Diagnostics")]
    [InlineData("AuthorizationGraphController", "AuthorizationGraph")]
    public void Normalize_RemovesControllerSuffix(string raw, string expected)
    {
        var result = ControllerNameNormalizer.Normalize(raw);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Users", "Users")]
    [InlineData("Controller", "Controller")]
    public void Normalize_WhenNoSuffix_ReturnsUnchanged(string raw, string expected)
    {
        var result = ControllerNameNormalizer.Normalize(raw);

        result.Should().Be(expected);
    }
}

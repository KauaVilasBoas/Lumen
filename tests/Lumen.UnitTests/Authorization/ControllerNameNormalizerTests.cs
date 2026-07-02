using Lumen.Authorization.AspNetCore;
using FluentAssertions;

namespace Lumen.UnitTests.Authorization;

public sealed class ControllerNameNormalizerTests
{
    [Theory]
    [InlineData("UsersController", "Users")]
    [InlineData("PermissionsController", "Permissions")]
    [InlineData("Controller", "Controller")]
    [InlineData("Users", "Users")]
    [InlineData("MyApiController", "MyApi")]
    public void Normalize_RemovesControllerSuffix(string input, string expected)
    {
        ControllerNameNormalizer.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_WithBlankInput_ThrowsArgumentException(string input)
    {
        var act = () => ControllerNameNormalizer.Normalize(input);

        act.Should().Throw<ArgumentException>();
    }
}

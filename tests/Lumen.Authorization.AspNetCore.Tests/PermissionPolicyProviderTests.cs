using FluentAssertions;
using Lumen.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.AspNetCore.Tests;

public sealed class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider BuildProvider()
    {
        var options = Options.Create(new AuthorizationOptions());
        return new PermissionPolicyProvider(options);
    }

    [Fact]
    public async Task GetPolicyAsync_WithDotNotation_ReturnsPermissionPolicy()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("Users.List");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should()
            .ContainSingle(req => req.Code == "Users.List");
    }

    [Fact]
    public async Task GetPolicyAsync_WithLumenPrefix_ReturnsPermissionPolicyWithStrippedCode()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("Lumen:Users.List");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should()
            .ContainSingle(req => req.Code == "Users.List");
    }

    [Fact]
    public async Task GetPolicyAsync_WithUnrecognizedName_ReturnsNull()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("SomeRandomPolicyName");

        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_WithLumenPrefixAndCode_ExtractsCodeCorrectly()
    {
        var provider = BuildProvider();

        var policy = await provider.GetPolicyAsync("Lumen:Profiles.Create");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should()
            .ContainSingle(req => req.Code == "Profiles.Create");
    }
}

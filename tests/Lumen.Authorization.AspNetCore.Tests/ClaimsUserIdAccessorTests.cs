using System.Security.Claims;
using FluentAssertions;
using Lumen.Authorization.AspNetCore;
using Lumen.Authorization.Contracts;
using Microsoft.Extensions.Options;

namespace Lumen.Authorization.AspNetCore.Tests;

public sealed class ClaimsUserIdAccessorTests
{
    private static IOptions<LumenAuthorizationOptions> DefaultOptions() =>
        Microsoft.Extensions.Options.Options.Create(new LumenAuthorizationOptions());

    private static IOptions<LumenAuthorizationOptions> OptionsWithClaimType(string claimType) =>
        Microsoft.Extensions.Options.Options.Create(new LumenAuthorizationOptions
        {
            UserIdClaimType = claimType
        });

    private static ClaimsPrincipal PrincipalWithClaim(string claimType, string value) =>
        new(new ClaimsIdentity([new Claim(claimType, value)]));

    [Fact]
    public void TryGetUserId_DefaultClaimType_ValidGuid_ReturnsTrueAndParsedId()
    {
        var accessor = new ClaimsUserIdAccessor(DefaultOptions());
        var expectedId = Guid.NewGuid();
        var principal = PrincipalWithClaim(ClaimTypes.NameIdentifier, expectedId.ToString());

        var result = accessor.TryGetUserId(principal, out var userId);

        result.Should().BeTrue();
        userId.Should().Be(expectedId);
    }

    [Fact]
    public void TryGetUserId_DefaultClaimType_AbsentClaim_ReturnsFalse()
    {
        var accessor = new ClaimsUserIdAccessor(DefaultOptions());
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = accessor.TryGetUserId(principal, out var userId);

        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetUserId_DefaultClaimType_InvalidGuid_ReturnsFalse()
    {
        var accessor = new ClaimsUserIdAccessor(DefaultOptions());
        var principal = PrincipalWithClaim(ClaimTypes.NameIdentifier, "not-a-guid");

        var result = accessor.TryGetUserId(principal, out var userId);

        result.Should().BeFalse();
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetUserId_CustomClaimType_ValidGuid_ReturnsTrueAndParsedId()
    {
        const string customClaimType = "custom_user_id";
        var accessor = new ClaimsUserIdAccessor(OptionsWithClaimType(customClaimType));
        var expectedId = Guid.NewGuid();
        var principal = PrincipalWithClaim(customClaimType, expectedId.ToString());

        var result = accessor.TryGetUserId(principal, out var userId);

        result.Should().BeTrue();
        userId.Should().Be(expectedId);
    }

    [Fact]
    public void TryGetUserId_CustomClaimType_DefaultClaimPresent_ReturnsFalse()
    {
        const string customClaimType = "custom_user_id";
        var accessor = new ClaimsUserIdAccessor(OptionsWithClaimType(customClaimType));
        var principal = PrincipalWithClaim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString());

        var result = accessor.TryGetUserId(principal, out var userId);

        result.Should().BeFalse(
            because: "the accessor must read from the configured claim type, not from ClaimTypes.NameIdentifier");
        userId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TryGetUserId_IsAssignableToIUserIdAccessor()
    {
        var accessor = new ClaimsUserIdAccessor(DefaultOptions());

        accessor.Should().BeAssignableTo<IUserIdAccessor>();
    }
}

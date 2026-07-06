using FluentAssertions;
using Lumen.Authorization.Domain;

namespace Lumen.Authorization.Tests.TenantScope;

/// <summary>
/// Validates the <see cref="UserProfile"/> domain entity scope behavior.
/// </summary>
public sealed class UserProfileScopeTests
{
    [Fact]
    public void Create_WithoutScope_ProducesGlobalAssignment()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var userProfile = UserProfile.Create(userId, profileId);

        userProfile.ScopeId.Should().BeNull(
            because: "the parameterless overload must produce a global assignment");
        userProfile.UserId.Should().Be(userId);
        userProfile.ProfileId.Should().Be(profileId);
    }

    [Fact]
    public void Create_WithNullScope_ProducesGlobalAssignment()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var userProfile = UserProfile.Create(userId, profileId, scopeId: null);

        userProfile.ScopeId.Should().BeNull();
    }

    [Fact]
    public void Create_WithScope_ProducesScopedAssignment()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();

        var userProfile = UserProfile.Create(userId, profileId, scopeId);

        userProfile.ScopeId.Should().Be(scopeId,
            because: "a scoped assignment must carry the provided scope identifier");
    }

    [Fact]
    public void Create_SameUserAndProfile_DifferentScopes_ProducesDistinctAssignments()
    {
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var scopeA = Guid.NewGuid();
        var scopeB = Guid.NewGuid();

        var globalAssignment = UserProfile.Create(userId, profileId);
        var scopedA = UserProfile.Create(userId, profileId, scopeA);
        var scopedB = UserProfile.Create(userId, profileId, scopeB);

        globalAssignment.ScopeId.Should().BeNull();
        scopedA.ScopeId.Should().Be(scopeA);
        scopedB.ScopeId.Should().Be(scopeB);

        // All three are semantically distinct assignments (different ScopeId).
        scopedA.ScopeId!.Value.Should().NotBe(scopedB.ScopeId!.Value);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        var scopeId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        var act = () => UserProfile.Create(Guid.Empty, profileId, scopeId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*UserId*");
    }

    [Fact]
    public void Create_WithEmptyProfileId_ThrowsArgumentException()
    {
        var userId = Guid.NewGuid();
        var scopeId = Guid.NewGuid();

        var act = () => UserProfile.Create(userId, Guid.Empty, scopeId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ProfileId*");
    }
}

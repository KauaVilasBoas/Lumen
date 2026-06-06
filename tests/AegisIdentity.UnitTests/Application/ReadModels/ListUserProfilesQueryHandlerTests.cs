using AegisIdentity.Domain.Authorization;
using AegisIdentity.ReadModels.Queries;
using FluentAssertions;
using NSubstitute;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class ListUserProfilesQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00");

    private readonly IUserProfileRepository _userProfileRepository = Substitute.For<IUserProfileRepository>();
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();

    [Fact]
    public async Task Handle_WhenUserHasNoAssignments_ReturnsEmptyList()
    {
        _userProfileRepository.ListByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UserProfile>());

        var result = await CreateHandler().Handle(new ListUserProfilesQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().BeEmpty();
        await _profileRepository.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserHasOneAssignment_ReturnsSingleResultWithCorrectFields()
    {
        var profile = Profile.Create("Administrator", "Full access profile");
        var assignment = UserProfile.Create(UserId, profile.Id);

        _userProfileRepository.ListByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { assignment });
        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { profile });

        var result = await CreateHandler().Handle(new ListUserProfilesQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].AssignmentId.Should().Be(assignment.Id);
        result[0].ProfileId.Should().Be(profile.Id);
        result[0].ProfileName.Should().Be("Administrator");
        result[0].IsSystem.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenProfileIsSystem_ReturnsTrueIsSystemFlag()
    {
        var sysProfile = Profile.Create("System", "System profile", isSystem: true);
        var assignment = UserProfile.Create(UserId, sysProfile.Id);

        _userProfileRepository.ListByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { assignment });
        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { sysProfile });

        var result = await CreateHandler().Handle(new ListUserProfilesQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserHasMultipleAssignments_ReturnsAllResults()
    {
        var profileA = Profile.Create("Admin", "Full access");
        var profileB = Profile.Create("Viewer", "Read-only access");
        var assignmentA = UserProfile.Create(UserId, profileA.Id);
        var assignmentB = UserProfile.Create(UserId, profileB.Id);

        _userProfileRepository.ListByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { assignmentA, assignmentB });
        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { profileA, profileB });

        var result = await CreateHandler().Handle(new ListUserProfilesQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().ContainSingle(r => r.ProfileId == profileA.Id && r.ProfileName == "Admin");
        result.Should().ContainSingle(r => r.ProfileId == profileB.Id && r.ProfileName == "Viewer");
    }

    [Fact]
    public async Task Handle_WhenAssignmentProfileNotFound_ExcludesThatAssignment()
    {
        var knownProfile = Profile.Create("User", "Basic access");
        var validAssignment = UserProfile.Create(UserId, knownProfile.Id);
        var orphanAssignment = UserProfile.Create(UserId, Guid.NewGuid());

        _userProfileRepository.ListByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { validAssignment, orphanAssignment });
        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { knownProfile });

        var result = await CreateHandler().Handle(new ListUserProfilesQueryHandler.Query(UserId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ProfileId.Should().Be(knownProfile.Id);
    }

    [Fact]
    public async Task Handle_DoesNotCallListAllAsync_UsesTwoFilteredQueries()
    {
        var profile = Profile.Create("Admin", "Full access");
        var assignment = UserProfile.Create(UserId, profile.Id);

        _userProfileRepository.ListByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(new[] { assignment });
        _profileRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { profile });

        await CreateHandler().Handle(new ListUserProfilesQueryHandler.Query(UserId), CancellationToken.None);

        await _profileRepository.DidNotReceive().ListAllAsync(Arg.Any<CancellationToken>());
        await _profileRepository.Received(1).GetByIdsAsync(
            Arg.Is<IReadOnlyList<Guid>>(ids => ids.Contains(profile.Id)),
            Arg.Any<CancellationToken>());
    }

    private ListUserProfilesQueryHandler CreateHandler() =>
        new(_userProfileRepository, _profileRepository);
}

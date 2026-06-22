using Lumen.CommandHandlers.Profiles.DeleteProfile;
using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using DomainProfile = Lumen.Domain.Authorization.Profile;

namespace Lumen.UnitTests.Application.Profiles;

public sealed class DeleteProfileCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IPublisher _publisher;
    private readonly DeleteProfileCommandHandler _sut;

    public DeleteProfileCommandHandlerTests()
    {
        _profileRepository = Substitute.For<IProfileRepository>();
        _userProfileRepository = Substitute.For<IUserProfileRepository>();
        _publisher = Substitute.For<IPublisher>();

        _sut = new DeleteProfileCommandHandler(
            _profileRepository,
            _userProfileRepository,
            _publisher);
    }

    // ── Guard: not found ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DomainProfile?)null);

        var act = async () => await _sut.Handle(
            new DeleteProfileCommandHandler.Command(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Guard: system profile (FIX-01) ───────────────────────────────────────

    [Fact]
    public async Task Handle_WhenSystemProfile_ThrowsForbiddenException()
    {
        var systemProfile = DomainProfile.Create("Admin", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        var act = async () => await _sut.Handle(
            new DeleteProfileCommandHandler.Command(systemProfile.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*cannot be deleted*");
    }

    [Fact]
    public async Task Handle_WhenSystemProfile_DoesNotCallCascadeDelete()
    {
        var systemProfile = DomainProfile.Create("Admin", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        try
        {
            await _sut.Handle(
                new DeleteProfileCommandHandler.Command(systemProfile.Id),
                CancellationToken.None);
        }
        catch (ForbiddenException) { }

        await _profileRepository.DidNotReceive()
            .DeleteWithCascadeAsync(
                Arg.Any<DomainProfile>(),
                Arg.Any<IReadOnlyList<PermissionProfile>>(),
                Arg.Any<IReadOnlyList<UserProfile>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSystemProfile_DoesNotPublishCacheInvalidation()
    {
        var systemProfile = DomainProfile.Create("Admin", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        try
        {
            await _sut.Handle(
                new DeleteProfileCommandHandler.Command(systemProfile.Id),
                CancellationToken.None);
        }
        catch (ForbiddenException) { }

        await _publisher.DidNotReceive()
            .Publish(Arg.Any<UserPermissionsChanged>(), Arg.Any<CancellationToken>());
    }

    // ── Atomic cascade ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithAssociations_CallsCascadeDeleteWithAllEntities()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var permission = Permission.Create("Docs", "Write", "Docs — Write");
        var permissionProfile = PermissionProfile.Create(permission.Id, profile.Id);
        var userId = Guid.NewGuid();
        var userProfile = UserProfile.Create(userId, profile.Id);

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId });
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile> { permissionProfile });
        _userProfileRepository.ListByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile> { userProfile });

        await _sut.Handle(new DeleteProfileCommandHandler.Command(profile.Id), CancellationToken.None);

        await _profileRepository.Received(1).DeleteWithCascadeAsync(
            Arg.Is<DomainProfile>(p => p.IsDeleted),
            Arg.Is<IReadOnlyList<PermissionProfile>>(list => list.Count == 1 && list[0].IsDeleted),
            Arg.Is<IReadOnlyList<UserProfile>>(list => list.Count == 1 && list[0].IsDeleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoAssociations_CallsCascadeDeleteWithEmptyLists()
    {
        var profile = DomainProfile.Create("Observers", "Observers profile");

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile>());
        _userProfileRepository.ListByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile>());

        await _sut.Handle(new DeleteProfileCommandHandler.Command(profile.Id), CancellationToken.None);

        await _profileRepository.Received(1).DeleteWithCascadeAsync(
            Arg.Is<DomainProfile>(p => p.IsDeleted),
            Arg.Is<IReadOnlyList<PermissionProfile>>(list => list.Count == 0),
            Arg.Is<IReadOnlyList<UserProfile>>(list => list.Count == 0),
            Arg.Any<CancellationToken>());
    }

    // ── Rollback / no partial state ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCascadeDeleteThrows_DoesNotPublishCacheInvalidation()
    {
        var profile = DomainProfile.Create("Finance", "Finance profile");
        var userId = Guid.NewGuid();

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId });
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile>());
        _userProfileRepository.ListByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile>());

        _profileRepository
            .DeleteWithCascadeAsync(
                Arg.Any<DomainProfile>(),
                Arg.Any<IReadOnlyList<PermissionProfile>>(),
                Arg.Any<IReadOnlyList<UserProfile>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Simulated DB failure"));

        var act = async () => await _sut.Handle(
            new DeleteProfileCommandHandler.Command(profile.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        // Cache invalidation must NOT fire when the transaction failed.
        await _publisher.DidNotReceive()
            .Publish(Arg.Any<UserPermissionsChanged>(), Arg.Any<CancellationToken>());
    }

    // ── Cache invalidation ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithAffectedUsers_PublishesPermissionsChangedForEach()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId1, userId2 });
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile>());
        _userProfileRepository.ListByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile>());

        await _sut.Handle(new DeleteProfileCommandHandler.Command(profile.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == userId1),
            Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == userId2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoAffectedUsers_DoesNotPublishAnyEvent()
    {
        var profile = DomainProfile.Create("Empty", "Profile with no users");

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile>());
        _userProfileRepository.ListByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile>());

        await _sut.Handle(new DeleteProfileCommandHandler.Command(profile.Id), CancellationToken.None);

        await _publisher.DidNotReceive()
            .Publish(Arg.Any<UserPermissionsChanged>(), Arg.Any<CancellationToken>());
    }
}

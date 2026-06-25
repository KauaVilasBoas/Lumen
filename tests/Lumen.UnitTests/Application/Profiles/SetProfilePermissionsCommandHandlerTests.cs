using Lumen.CommandHandlers.Profiles.SetProfilePermissions;
using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using DomainProfile = Lumen.Domain.Authorization.Profile;

namespace Lumen.UnitTests.Application.Profiles;

public sealed class SetProfilePermissionsCommandHandlerTests
{
    private readonly IProfileRepository _profileRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly SetProfilePermissionsCommandHandler _sut;

    public SetProfilePermissionsCommandHandlerTests()
    {
        _profileRepository = Substitute.For<IProfileRepository>();
        _permissionRepository = Substitute.For<IPermissionRepository>();

        _sut = new SetProfilePermissionsCommandHandler(
            _profileRepository,
            _permissionRepository);
    }

    [Fact]
    public async Task Handle_WhenProfileNotFound_ThrowsNotFoundException()
    {
        _profileRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((DomainProfile?)null);

        var act = async () => await _sut.Handle(
            new SetProfilePermissionsCommandHandler.Command(Guid.NewGuid(), new List<Guid>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenSystemProfile_ThrowsForbiddenException()
    {
        var systemProfile = DomainProfile.Create("Administrator", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        var act = async () => await _sut.Handle(
            new SetProfilePermissionsCommandHandler.Command(systemProfile.Id, new List<Guid> { Guid.NewGuid() }),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*managed automatically*");
    }

    [Fact]
    public async Task Handle_WhenSystemProfile_DoesNotWriteToRepository()
    {
        var systemProfile = DomainProfile.Create("Administrator", "System profile", isSystem: true);

        _profileRepository.FindByIdAsync(systemProfile.Id, Arg.Any<CancellationToken>())
            .Returns(systemProfile);

        try
        {
            await _sut.Handle(
                new SetProfilePermissionsCommandHandler.Command(systemProfile.Id, new List<Guid> { Guid.NewGuid() }),
                CancellationToken.None);
        }
        catch (ForbiddenException) { }

        await _profileRepository.DidNotReceive()
            .UpdatePermissionProfileAsync(Arg.Any<PermissionProfile>(), Arg.Any<CancellationToken>());
        await _profileRepository.DidNotReceive()
            .InsertPermissionProfilesAsync(Arg.Any<IReadOnlyList<PermissionProfile>>(), Arg.Any<CancellationToken>());
        systemProfile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNonSystemProfile_PermissionNotFound_ThrowsNotFoundException()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var missingPermissionId = Guid.NewGuid();

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _permissionRepository.FindByIdAsync(missingPermissionId, Arg.Any<CancellationToken>())
            .Returns((Permission?)null);

        var act = async () => await _sut.Handle(
            new SetProfilePermissionsCommandHandler.Command(profile.Id, new List<Guid> { missingPermissionId }),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNonSystemProfile_AddsNewPermissionsAndRemovesDropped()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var permIdToKeep = Guid.NewGuid();
        var permIdToAdd = Guid.NewGuid();
        var permIdToRemove = Guid.NewGuid();

        var permToKeep = Permission.Create("Docs", "Read", "Docs.Read");
        var permToAdd = Permission.Create("Docs", "Write", "Docs.Write");

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);

        _permissionRepository.FindByIdAsync(permIdToKeep, Arg.Any<CancellationToken>())
            .Returns(permToKeep);
        _permissionRepository.FindByIdAsync(permIdToAdd, Arg.Any<CancellationToken>())
            .Returns(permToAdd);

        var existingToRemove = PermissionProfile.Create(permIdToRemove, profile.Id);
        var existingToKeep  = PermissionProfile.Create(permIdToKeep, profile.Id);

        _profileRepository
            .GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile> { existingToRemove, existingToKeep });

        _profileRepository
            .GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        await _sut.Handle(
            new SetProfilePermissionsCommandHandler.Command(profile.Id, new List<Guid> { permIdToKeep, permIdToAdd }),
            CancellationToken.None);

        await _profileRepository.Received(1).UpdatePermissionProfileAsync(
            Arg.Is<PermissionProfile>(pp => pp.PermissionId == permIdToRemove && pp.IsDeleted),
            Arg.Any<CancellationToken>());

        await _profileRepository.Received(1).InsertPermissionProfilesAsync(
            Arg.Is<IReadOnlyList<PermissionProfile>>(list =>
                list.Count == 1 && list[0].PermissionId == permIdToAdd),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNonSystemProfile_RaisesProfilePermissionsSetWithAffectedUsers()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");
        var userId = Guid.NewGuid();

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile>());
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { userId });

        await _sut.Handle(
            new SetProfilePermissionsCommandHandler.Command(profile.Id, new List<Guid>(), "actor-user"),
            CancellationToken.None);

        await _profileRepository.Received(1).UpdateAsync(profile, Arg.Any<CancellationToken>());

        profile.DomainEvents.Should().ContainSingle(e => e is ProfilePermissionsSet
            && ((ProfilePermissionsSet)e).ProfileId == profile.Id
            && ((ProfilePermissionsSet)e).ProfileName == profile.Name
            && ((ProfilePermissionsSet)e).ActorUsername == "actor-user"
            && ((ProfilePermissionsSet)e).AffectedUserIds.SequenceEqual(new[] { userId }));
    }

    // ── Validator unit tests ──────────────────────────────────────────────────

    private readonly SetProfilePermissionsCommandHandler.Validator _validator = new();

    [Fact]
    public async Task Validator_WhenPermissionIdsIsNull_FailsWithRequiredMessage()
    {
        var command = new SetProfilePermissionsCommandHandler.Command(Guid.NewGuid(), null!);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.PermissionIds)
            .WithErrorMessage("PermissionIds is required.");
    }

    [Fact]
    public async Task Validator_WhenPermissionIdsIsEmpty_PassesListRule()
    {
        var command = new SetProfilePermissionsCommandHandler.Command(Guid.NewGuid(), new List<Guid>());

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(c => c.PermissionIds);
    }

    [Fact]
    public async Task Validator_WhenPermissionIdsContainsEmptyGuid_FailsItemRule()
    {
        var command = new SetProfilePermissionsCommandHandler.Command(
            Guid.NewGuid(),
            new List<Guid> { Guid.Empty });

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("PermissionIds[0]")
            .WithErrorMessage("Each PermissionId must be a valid non-empty Guid.");
    }

    [Fact]
    public async Task Validator_WhenProfileIdIsEmpty_FailsRequiredMessage()
    {
        var command = new SetProfilePermissionsCommandHandler.Command(Guid.Empty, new List<Guid>());

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ProfileId)
            .WithErrorMessage("ProfileId is required.");
    }

    [Fact]
    public async Task Validator_WhenCommandIsFullyValid_HasNoErrors()
    {
        var command = new SetProfilePermissionsCommandHandler.Command(
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid() });

        var result = await _validator.TestValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}

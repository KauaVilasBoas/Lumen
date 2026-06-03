using AegisIdentity.CommandHandlers.Profiles.DeleteProfile;
using AegisIdentity.Domain.Authorization;
using AegisIdentity.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using DomainProfile = AegisIdentity.Domain.Authorization.Profile;

namespace AegisIdentity.UnitTests.Application.Profiles;

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
    public async Task Handle_WhenSystemProfile_DoesNotTouchRepositoryAfterGuard()
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
            .UpdateAsync(Arg.Any<DomainProfile>(), Arg.Any<CancellationToken>());
        await _userProfileRepository.DidNotReceive()
            .UpdateAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>());
    }

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
    public async Task Handle_SoftDeletesProfile_UpdatesRepository()
    {
        var profile = DomainProfile.Create("Editors", "Editors profile");

        _profileRepository.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(profile);
        _profileRepository.GetUserIdsByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());
        _profileRepository.GetActivePermissionProfilesByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<PermissionProfile>());
        _userProfileRepository.ListByProfileIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new List<UserProfile>());

        await _sut.Handle(new DeleteProfileCommandHandler.Command(profile.Id), CancellationToken.None);

        await _profileRepository.Received(1).UpdateAsync(
            Arg.Is<DomainProfile>(p => p.IsDeleted),
            Arg.Any<CancellationToken>());
    }
}

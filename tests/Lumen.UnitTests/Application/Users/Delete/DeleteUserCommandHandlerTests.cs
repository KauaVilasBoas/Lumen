using Lumen.CommandHandlers.Users.Delete;
using Lumen.Domain.Audit;
using Lumen.Domain.Authorization;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Users.Delete;

public sealed class DeleteUserCommandHandlerTests
{
    private const string ActorId = "00000000-0000-0000-0000-000000000099";
    private const string FakeHash = "$2a$12$fakehash";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserProfileRepository _userProfileRepository = Substitute.For<IUserProfileRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly User _activeUser;

    public DeleteUserCommandHandlerTests()
    {
        _activeUser = User.Create("alice@example.com", "alice", FakeHash);

        _userRepository.FindByIdAsync(_activeUser.Id, Arg.Any<CancellationToken>())
            .Returns(_activeUser);

        _userProfileRepository
            .FindActiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        _refreshTokenRepository
            .FindByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken>());
    }

    // ── 404 — user not found ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(Guid.NewGuid(), ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── 403 — bootstrap user ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsBootstrap_ThrowsForbiddenException()
    {
        var bootstrapUser = User.CreateBootstrap("admin@example.com", "admin", FakeHash);
        _userRepository.FindByIdAsync(bootstrapUser.Id, Arg.Any<CancellationToken>())
            .Returns(bootstrapUser);

        var act = () => CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(bootstrapUser.Id, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*bootstrap*");
    }

    [Fact]
    public async Task Handle_WhenUserIsBootstrap_DoesNotSoftDelete()
    {
        var bootstrapUser = User.CreateBootstrap("admin@example.com", "admin", FakeHash);
        _userRepository.FindByIdAsync(bootstrapUser.Id, Arg.Any<CancellationToken>())
            .Returns(bootstrapUser);

        try { await CreateHandler().Handle(new DeleteUserCommandHandler.Command(bootstrapUser.Id, ActorId), CancellationToken.None); }
        catch (ForbiddenException) { }

        bootstrapUser.IsDeleted.Should().BeFalse();
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── 409 — last administrator ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDeletingLastAdministrator_ThrowsConflictException()
    {
        _userProfileRepository
            .FindActiveAsync(_activeUser.Id, SystemProfiles.AdministratorId, Arg.Any<CancellationToken>())
            .Returns(UserProfile.Create(_activeUser.Id, SystemProfiles.AdministratorId));

        _userRepository
            .CountActiveAdministratorsAsync(SystemProfiles.AdministratorId, Arg.Any<CancellationToken>())
            .Returns(1);

        var act = () => CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenDeletingNonLastAdministrator_DoesNotThrow()
    {
        _userProfileRepository
            .FindActiveAsync(_activeUser.Id, SystemProfiles.AdministratorId, Arg.Any<CancellationToken>())
            .Returns(UserProfile.Create(_activeUser.Id, SystemProfiles.AdministratorId));

        _userRepository
            .CountActiveAdministratorsAsync(SystemProfiles.AdministratorId, Arg.Any<CancellationToken>())
            .Returns(2);

        var act = () => CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── soft delete applied ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_SetsUserAsDeleted()
    {
        await CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        _activeUser.IsDeleted.Should().BeTrue();
        _activeUser.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenValid_CallsUpdateRepository()
    {
        await CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(_activeUser, Arg.Any<CancellationToken>());
    }

    // ── refresh token revocation ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_RevokesActiveRefreshTokens()
    {
        var activeToken = RefreshToken.Create(_activeUser.Id, "hash1", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        var revokedToken = RefreshToken.Create(_activeUser.Id, "hash2", DateTime.UtcNow.AddDays(7), "127.0.0.1");
        revokedToken.Revoke();

        _refreshTokenRepository.FindByUserIdAsync(_activeUser.Id, Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { activeToken, revokedToken });

        await CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await _refreshTokenRepository.Received(1).UpdateAsync(activeToken, Arg.Any<CancellationToken>());
        await _refreshTokenRepository.DidNotReceive().UpdateAsync(revokedToken, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoActiveRefreshTokens_DoesNotCallTokenUpdate()
    {
        _refreshTokenRepository.FindByUserIdAsync(_activeUser.Id, Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken>());

        await CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await _refreshTokenRepository.DidNotReceive().UpdateAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    // ── audit trail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_InsertsAuditEntry()
    {
        await CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.UserDeleted &&
                e.Actor == ActorId &&
                e.Target == _activeUser.Id.ToString()),
            Arg.Any<CancellationToken>());
    }

    // ── permission cache invalidation ─────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_PublishesUserPermissionsChanged()
    {
        await CreateHandler().Handle(
            new DeleteUserCommandHandler.Command(_activeUser.Id, ActorId),
            CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<UserPermissionsChanged>(e => e.UserId == _activeUser.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGuardFails_DoesNotPublishCacheInvalidation()
    {
        var bootstrapUser = User.CreateBootstrap("admin@example.com", "admin", FakeHash);
        _userRepository.FindByIdAsync(bootstrapUser.Id, Arg.Any<CancellationToken>())
            .Returns(bootstrapUser);

        try { await CreateHandler().Handle(new DeleteUserCommandHandler.Command(bootstrapUser.Id, ActorId), CancellationToken.None); }
        catch (ForbiddenException) { }

        await _publisher.DidNotReceive().Publish(Arg.Any<UserPermissionsChanged>(), Arg.Any<CancellationToken>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private DeleteUserCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _userProfileRepository,
            _refreshTokenRepository,
            _auditRepository,
            _publisher,
            NullLogger<DeleteUserCommandHandler>.Instance);
}

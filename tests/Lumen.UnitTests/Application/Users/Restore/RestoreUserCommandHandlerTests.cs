using Lumen.CommandHandlers.Users.Restore;
using Lumen.Domain.Audit;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Application.Users.Restore;

public sealed class RestoreUserCommandHandlerTests
{
    private const string ActorId = "00000000-0000-0000-0000-000000000099";
    private const string FakeHash = "$2a$12$fakehash";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();

    private readonly User _deletedUser;

    public RestoreUserCommandHandlerTests()
    {
        _deletedUser = User.Create("bob@example.com", "bob", FakeHash);
        _deletedUser.SoftDelete();

        _userRepository
            .FindByIdIgnoringFiltersAsync(_deletedUser.Id, Arg.Any<CancellationToken>())
            .Returns(_deletedUser);
    }

    // ── 404 — user not found ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _userRepository
            .FindByIdIgnoringFiltersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(
            new RestoreUserCommandHandler.Command(Guid.NewGuid(), ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── 404 — user not deleted ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserIsNotDeleted_ThrowsNotFoundException()
    {
        var activeUser = User.Create("carol@example.com", "carol", FakeHash);
        _userRepository
            .FindByIdIgnoringFiltersAsync(activeUser.Id, Arg.Any<CancellationToken>())
            .Returns(activeUser);

        var act = () => CreateHandler().Handle(
            new RestoreUserCommandHandler.Command(activeUser.Id, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── 409 — restore window expired ─────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenRestoreWindowExpired_ThrowsConflictException()
    {
        var expiredUser = User.Create("eve@example.com", "eve", FakeHash);
        expiredUser.SoftDelete();

        var expiredDate = DateTime.UtcNow.AddDays(-(ValidationLimits.UserRestoreWindowDays + 1));

        var backingField = typeof(User)
            .GetField("<DeletedAt>k__BackingField",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        backingField!.SetValue(expiredUser, expiredDate);

        _userRepository
            .FindByIdIgnoringFiltersAsync(expiredUser.Id, Arg.Any<CancellationToken>())
            .Returns(expiredUser);

        var act = () => CreateHandler().Handle(
            new RestoreUserCommandHandler.Command(expiredUser.Id, ActorId),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── restore applied ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_ClearsDeletedFlag()
    {
        await CreateHandler().Handle(
            new RestoreUserCommandHandler.Command(_deletedUser.Id, ActorId),
            CancellationToken.None);

        _deletedUser.IsDeleted.Should().BeFalse();
        _deletedUser.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenValid_CallsUpdateRepository()
    {
        await CreateHandler().Handle(
            new RestoreUserCommandHandler.Command(_deletedUser.Id, ActorId),
            CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(_deletedUser, Arg.Any<CancellationToken>());
    }

    // ── audit trail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_InsertsAuditEntry()
    {
        await CreateHandler().Handle(
            new RestoreUserCommandHandler.Command(_deletedUser.Id, ActorId),
            CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Kind == AuditEventKinds.UserRestored &&
                e.Actor == ActorId &&
                e.Target == _deletedUser.Id.ToString()),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private RestoreUserCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _auditRepository,
            NullLogger<RestoreUserCommandHandler>.Instance);
}

using Lumen.CommandHandlers.Users.Update;
using Lumen.Domain.Audit;
using Lumen.Domain.Notifications;
using Lumen.Domain.Tokens;
using Lumen.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.UnitTests.Application.Users;

public sealed class UpdateUserCommandHandlerTests
{
    private const string ActorId         = "00000000-0000-0000-0000-000000000001";
    private const string ExistingEmail    = "alice@example.com";
    private const string ExistingUsername = "alice";
    private const string FakeHash        = "$2a$12$fakehash";

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailConfirmationTokenRepository _tokenRepository = Substitute.For<IEmailConfirmationTokenRepository>();
    private readonly IEmailConfirmationService _emailConfirmationService = Substitute.For<IEmailConfirmationService>();
    private readonly IAuditRepository _auditRepository = Substitute.For<IAuditRepository>();

    private readonly User _existingUser;

    public UpdateUserCommandHandlerTests()
    {
        _existingUser = User.Create(ExistingEmail, ExistingUsername, FakeHash);

        _userRepository.FindByIdAsync(_existingUser.Id, Arg.Any<CancellationToken>())
            .Returns(_existingUser);
    }

    // ── 404 — user not found ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var act = () => CreateHandler().Handle(ValidUsernameCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── 409 — duplicate username ──────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNewUsernameAlreadyInUse_ThrowsConflictException()
    {
        var otherUser = User.Create("other@example.com", "other", FakeHash);
        _userRepository.FindByUsernameAsync("taken", Arg.Any<CancellationToken>())
            .Returns(otherUser);

        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: null,
            NewUsername: "taken",
            ActorId: ActorId);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── 409 — duplicate email ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNewEmailAlreadyInUse_ThrowsConflictException()
    {
        var otherUser = User.Create("taken@example.com", "other", FakeHash);
        _userRepository.FindByEmailAsync("taken@example.com", Arg.Any<CancellationToken>())
            .Returns(otherUser);

        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: "taken@example.com",
            NewUsername: null,
            ActorId: ActorId);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── no-op when nothing changes ────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoFieldChanges_DoesNotCallUpdate()
    {
        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: ExistingEmail,
            NewUsername: ExistingUsername,
            ActorId: ActorId);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.EmailChanged.Should().BeFalse();
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _auditRepository.DidNotReceive().InsertAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    // ── username update ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUsernameChanges_UpdatesUserAndAudits()
    {
        var result = await CreateHandler().Handle(ValidUsernameCommand(_existingUser.Id), CancellationToken.None);

        result.UserId.Should().Be(_existingUser.Id);
        result.EmailChanged.Should().BeFalse();
        await _userRepository.Received(1).UpdateAsync(_existingUser, Arg.Any<CancellationToken>());
        await _auditRepository.Received(1).InsertAsync(Arg.Any<AuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUsernameChanges_DoesNotSendEmail()
    {
        await CreateHandler().Handle(ValidUsernameCommand(_existingUser.Id), CancellationToken.None);

        await _emailConfirmationService.DidNotReceive()
            .SendConfirmationEmailAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ── email update ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailChanges_SetsIsActiveFalse()
    {
        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: "newemail@example.com",
            NewUsername: null,
            ActorId: ActorId);

        await CreateHandler().Handle(command, CancellationToken.None);

        _existingUser.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenEmailChanges_ReturnsEmailChangedTrue()
    {
        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: "newemail@example.com",
            NewUsername: null,
            ActorId: ActorId);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.EmailChanged.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenEmailChanges_InvalidatesPreviousTokensAndDelegatesToService()
    {
        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: "newemail@example.com",
            NewUsername: null,
            ActorId: ActorId);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _tokenRepository.Received(1).InvalidateByUserIdAsync(_existingUser.Id, Arg.Any<CancellationToken>());
        await _emailConfirmationService.Received(1)
            .SendConfirmationEmailAsync(_existingUser, Arg.Any<CancellationToken>());
    }

    // ── audit entry content ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUsernameChanges_AuditEntryContainsActorAndTarget()
    {
        await CreateHandler().Handle(ValidUsernameCommand(_existingUser.Id), CancellationToken.None);

        await _auditRepository.Received(1).InsertAsync(
            Arg.Is<AuditEntry>(e =>
                e.Actor == ActorId &&
                e.Target == _existingUser.Id.ToString()),
            Arg.Any<CancellationToken>());
    }

    // ── repository conflict rethrown as ConflictException ─────────────────

    [Fact]
    public async Task Handle_WhenUpdateThrowsDuplicateEmail_ThrowsConflictException()
    {
        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEmailException("newemail@example.com"));

        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: "newemail@example.com",
            NewUsername: null,
            ActorId: ActorId);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_WhenUpdateThrowsDuplicateUsername_ThrowsConflictException()
    {
        _userRepository.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateUsernameException("newusername"));

        var command = new UpdateUserCommandHandler.Command(
            UserId: _existingUser.Id,
            NewEmail: null,
            NewUsername: "newusername",
            ActorId: ActorId);

        var act = () => CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private UpdateUserCommandHandler CreateHandler() =>
        new(
            _userRepository,
            _tokenRepository,
            _emailConfirmationService,
            _auditRepository,
            NullLogger<UpdateUserCommandHandler>.Instance);

    private static UpdateUserCommandHandler.Command ValidUsernameCommand(Guid userId) =>
        new(
            UserId: userId,
            NewEmail: null,
            NewUsername: "alice_updated",
            ActorId: ActorId);
}

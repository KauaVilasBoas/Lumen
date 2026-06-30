using FluentAssertions;
using Lumen.Modules.Identity.Application.Users.Restore;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class RestoreUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private RestoreUserCommandHandler CreateHandler()
        => new(_userRepository, NullLogger<RestoreUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_DeletedUserWithinRestoreWindow_RestoresUser()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();
        user.SoftDelete();

        _userRepository
            .FindByIdIgnoringFiltersAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = CreateHandler();
        await handler.Handle(new RestoreUserCommand(user.Id, "admin"), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(
            Arg.Is<User>(u => !u.IsDeleted),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository
            .FindByIdIgnoringFiltersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RestoreUserCommand(Guid.NewGuid(), "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ActiveUser_ThrowsNotFoundException()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.ConfirmEmail();

        _userRepository
            .FindByIdIgnoringFiltersAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RestoreUserCommand(user.Id, "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedUserOutsideRestoreWindow_ThrowsConflictException()
    {
        var user = CreateUserDeletedLongAgo();

        _userRepository
            .FindByIdIgnoringFiltersAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new RestoreUserCommand(user.Id, "admin"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static User CreateUserDeletedLongAgo()
    {
        var user = User.Create("alice@test.com", "alice", "hash");
        user.SoftDelete();

        typeof(User)
            .GetProperty(nameof(User.DeletedAt))!
            .SetValue(user, DateTime.UtcNow.AddDays(-400));

        return user;
    }
}

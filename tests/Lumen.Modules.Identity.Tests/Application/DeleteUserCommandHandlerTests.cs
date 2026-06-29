using FluentAssertions;
using Lumen.Modularity;
using Lumen.Modules.Identity.Application.Users.Delete;
using Lumen.Modules.Identity.Contracts.Events;
using Lumen.Modules.Identity.Domain.Authorization;
using Lumen.Modules.Identity.Domain.Tokens;
using Lumen.Modules.Identity.Domain.Users;
using Lumen.SharedKernel.Constants;
using Lumen.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class DeleteUserCommandHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserProfileRepository _userProfileRepository = Substitute.For<IUserProfileRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private DeleteUserCommandHandler CreateHandler()
        => new(_userRepository, _userProfileRepository, _refreshTokenRepository, _eventBus, NullLogger<DeleteUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidUser_SoftDeletesAndPublishesPermissionsChangedEvent()
    {
        var user = User.Create("user@test.com", "user", "hash");
        user.ConfirmEmail();
        var userId = user.Id;

        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);
        _userProfileRepository.FindActiveAsync(userId, SystemProfiles.AdministratorId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        _refreshTokenRepository.FindByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns([]);

        var handler = CreateHandler();
        await handler.Handle(new DeleteUserCommand(userId, "admin"), CancellationToken.None);

        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.IsDeleted), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<UserPermissionsChangedEvent>(e => e.UserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BootstrapUser_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var user = User.CreateBootstrap("admin@test.com", "admin", "hash");

        _userRepository.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new DeleteUserCommand(userId, "actor"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.FindByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var handler = CreateHandler();
        var act = async () => await handler.Handle(new DeleteUserCommand(Guid.NewGuid(), "actor"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

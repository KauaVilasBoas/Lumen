using Lumen.DataAccess.Cache;
using Lumen.Domain.Authorization;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Lumen.UnitTests.Authorization;

public sealed class UserPermissionServiceTests
{
    private readonly IUserPermissionCache _cache;
    private readonly IProfileRepository _profileRepository;
    private readonly UserPermissionService _sut;

    public UserPermissionServiceTests()
    {
        _cache = Substitute.For<IUserPermissionCache>();
        _profileRepository = Substitute.For<IProfileRepository>();

        _sut = new UserPermissionService(
            _cache,
            _profileRepository,
            NullLogger<UserPermissionService>.Instance);
    }

    [Fact]
    public async Task GetPermissionsAsync_CacheHit_ReturnsCachedSet()
    {
        var userId = Guid.NewGuid();
        var cached = new HashSet<string> { "Users.Delete" };
        _cache.GetAsync(userId).Returns(cached);

        var result = await _sut.GetPermissionsAsync(userId);

        result.Should().BeSameAs(cached);
        await _profileRepository.DidNotReceive().GetPermissionCodesByUserIdAsync(userId);
    }

    [Fact]
    public async Task GetPermissionsAsync_CacheMiss_QueriesDbAndPopulatesCache()
    {
        var userId = Guid.NewGuid();
        var fromDb = new HashSet<string> { "Admin.Ping", "Users.Delete" };
        _cache.GetAsync(userId).Returns((HashSet<string>?)null);
        _profileRepository.GetPermissionCodesByUserIdAsync(userId).Returns(fromDb);

        var result = await _sut.GetPermissionsAsync(userId);

        result.Should().BeEquivalentTo(fromDb);
        await _cache.Received(1).SetAsync(userId, fromDb);
    }

    [Fact]
    public async Task HasPermissionAsync_UserHasPermission_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _cache.GetAsync(userId).Returns(new HashSet<string> { "Admin.Ping" });

        var result = await _sut.HasPermissionAsync(userId, "Admin.Ping");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UserLacksPermission_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _cache.GetAsync(userId).Returns(new HashSet<string> { "Admin.Ping" });

        var result = await _sut.HasPermissionAsync(userId, "Users.Delete");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_EmptyPermissionSet_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _cache.GetAsync(userId).Returns(new HashSet<string>());

        var result = await _sut.HasPermissionAsync(userId, "Admin.Ping");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_PermissionCodeIsCaseSensitive()
    {
        var userId = Guid.NewGuid();
        _cache.GetAsync(userId).Returns(new HashSet<string> { "Admin.Ping" });

        var result = await _sut.HasPermissionAsync(userId, "admin.ping");

        result.Should().BeFalse();
    }
}

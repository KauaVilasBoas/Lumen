using Lumen.Jobs.Jobs;
using Lumen.Modularity;
using Lumen.Modules.Audit.Contracts.Events;
using Lumen.Modules.Identity.Application.Tokens;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Lumen.UnitTests.Jobs;

public sealed class CleanupExpiredRefreshTokensJobTests
{
    private readonly ITokenCleanupService _tokenCleanupService;
    private readonly IEventBus _eventBus;
    private readonly CleanupExpiredRefreshTokensJob _sut;

    public CleanupExpiredRefreshTokensJobTests()
    {
        _tokenCleanupService = Substitute.For<ITokenCleanupService>();
        _eventBus = Substitute.For<IEventBus>();
        _sut = new CleanupExpiredRefreshTokensJob(
            _tokenCleanupService,
            _eventBus,
            NullLogger<CleanupExpiredRefreshTokensJob>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_CallsDeleteExpiredAsync_WithUtcCutoffNoEarlierThanNow()
    {
        var before = DateTime.UtcNow;
        _tokenCleanupService.DeleteExpiredRefreshTokensAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                            .Returns(0);

        await _sut.ExecuteAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        await _tokenCleanupService.Received(1).DeleteExpiredRefreshTokensAsync(
            Arg.Is<DateTime>(cutoff =>
                cutoff >= before.AddSeconds(-1) && cutoff <= after.AddSeconds(1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCleanupDeletesTokens_PublishesCleanupJobExecutedEvent()
    {
        const int deletedCount = 42;
        _tokenCleanupService.DeleteExpiredRefreshTokensAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                            .Returns(deletedCount);

        await _sut.ExecuteAsync(CancellationToken.None);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<CleanupJobExecutedEvent>(e => e.DeletedCount == deletedCount),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCleanupServiceThrows_PropagatesException()
    {
        _tokenCleanupService.DeleteExpiredRefreshTokensAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var act = async () => await _sut.ExecuteAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("storage unavailable");
    }
}

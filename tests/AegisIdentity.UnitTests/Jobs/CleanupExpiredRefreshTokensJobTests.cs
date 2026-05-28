using AegisIdentity.Domain.Tokens;
using AegisIdentity.Jobs.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AegisIdentity.UnitTests.Jobs;

public sealed class CleanupExpiredRefreshTokensJobTests
{
    private readonly IRefreshTokenRepository _repository;
    private readonly CleanupExpiredRefreshTokensJob _sut;

    public CleanupExpiredRefreshTokensJobTests()
    {
        _repository = Substitute.For<IRefreshTokenRepository>();
        _sut = new CleanupExpiredRefreshTokensJob(
            _repository,
            NullLogger<CleanupExpiredRefreshTokensJob>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_CallsDeleteExpiredAsync_WithUtcCutoffNoEarlierThanNow()
    {
        // Arrange
        var before = DateTime.UtcNow;
        _repository.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                   .Returns(0L);

        // Act
        await _sut.ExecuteAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        // Assert
        await _repository.Received(1).DeleteExpiredAsync(
            Arg.Is<DateTime>(cutoff =>
                cutoff >= before.AddSeconds(-1) && cutoff <= after.AddSeconds(1)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryDeletesTokens_DoesNotThrow()
    {
        // Arrange
        const long deletedCount = 42L;
        _repository.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                   .Returns(deletedCount);

        // Act
        var act = async () => await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_PropagatesException()
    {
        // Arrange
        _repository.DeleteExpiredAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                   .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));

        // Act
        var act = async () => await _sut.ExecuteAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("Mongo unavailable");
    }
}

using Lumen.ReadModels.Queries;
using FluentAssertions;

namespace Lumen.UnitTests.Application.ReadModels;

public sealed class GetRecentAuditFeedQueryValidatorTests
{
    private readonly GetRecentAuditFeedQueryHandler.Validator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_TakeInRange_PassesValidation(int take)
    {
        var query = new GetRecentAuditFeedQueryHandler.Query(take);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(500)]
    public void Validate_TakeOutOfRange_FailsValidation(int take)
    {
        var query = new GetRecentAuditFeedQueryHandler.Query(take);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "take");
    }
}

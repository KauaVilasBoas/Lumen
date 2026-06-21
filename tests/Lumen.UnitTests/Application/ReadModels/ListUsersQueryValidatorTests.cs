using AegisIdentity.ReadModels.Queries;
using FluentAssertions;

namespace AegisIdentity.UnitTests.Application.ReadModels;

public sealed class ListUsersQueryValidatorTests
{
    private readonly ListUsersQueryHandler.Validator _validator = new();

    [Theory]
    [InlineData("active")]
    [InlineData("locked")]
    [InlineData("pending")]
    [InlineData("deleted")]
    [InlineData("all")]
    [InlineData("ACTIVE")]
    [InlineData("ALL")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ValidStateValues_PassesValidation(string? state)
    {
        var query = new ListUsersQueryHandler.Query(null, state, 1, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("suspend")]
    [InlineData("banned")]
    public void Validate_InvalidState_FailsValidation(string state)
    {
        var query = new ListUsersQueryHandler.Query(null, state, 1, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "state");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_PageLessThanOne_FailsValidation(int page)
    {
        var query = new ListUsersQueryHandler.Query(null, null, page, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "page");
    }

    [Fact]
    public void Validate_PageOne_PassesValidation()
    {
        var query = new ListUsersQueryHandler.Query(null, null, 1, 20);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(200)]
    public void Validate_PageSizeOutOfRange_FailsValidation(int pageSize)
    {
        var query = new ListUsersQueryHandler.Query(null, null, 1, pageSize);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "pageSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_PageSizeInRange_PassesValidation(int pageSize)
    {
        var query = new ListUsersQueryHandler.Query(null, null, 1, pageSize);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleViolations_ReturnsAllErrors()
    {
        var query = new ListUsersQueryHandler.Query(null, "invalid", 0, 0);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }
}

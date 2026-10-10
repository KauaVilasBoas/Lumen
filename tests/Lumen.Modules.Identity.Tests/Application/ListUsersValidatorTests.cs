using FluentValidation.TestHelper;
using Lumen.Modules.Identity.Application.Queries;
using Lumen.SharedKernel.Constants;

namespace Lumen.Modules.Identity.Tests.Application;

public sealed class ListUsersValidatorTests
{
    private readonly ListUsersQueryHandler.Validator _validator = new();

    private static ListUsersQuery ValidQuery(string? state = null)
        => new(Search: null, State: state, Page: 1, PageSize: 10);

    [Fact]
    public void Validate_NullState_HasNoStateError()
    {
        var result = _validator.TestValidate(ValidQuery(state: null));
        result.ShouldNotHaveValidationErrorFor("state");
    }

    [Fact]
    public void Validate_EmptyState_HasNoStateError()
    {
        var result = _validator.TestValidate(ValidQuery(state: ""));
        result.ShouldNotHaveValidationErrorFor("state");
    }

    [Fact]
    public void Validate_AllFilterSentinel_HasNoStateError()
    {
        var result = _validator.TestValidate(ValidQuery(state: UserStates.AllFilter));
        result.ShouldNotHaveValidationErrorFor("state");
    }

    [Theory]
    [MemberData(nameof(CanonicalStates))]
    public void Validate_EachCanonicalState_HasNoStateError(string state)
    {
        var result = _validator.TestValidate(ValidQuery(state: state));
        result.ShouldNotHaveValidationErrorFor("state");
    }

    [Fact]
    public void Validate_UnknownState_ProducesStateError()
    {
        var result = _validator.TestValidate(ValidQuery(state: "suspended"));
        result.ShouldHaveValidationErrorFor("state");
    }

    public static TheoryData<string> CanonicalStates()
    {
        var data = new TheoryData<string>();
        foreach (var state in UserStates.AllCanonical)
            data.Add(state);
        return data;
    }
}

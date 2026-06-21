using AegisIdentity.CommandHandlers.Behaviors;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;

namespace AegisIdentity.UnitTests.Application.Behaviors;

public sealed class ValidationBehaviorTests
{
    // ── Minimal stubs ─────────────────────────────────────────────────────────

    private sealed record TestCommand(string Value) : IRequest<string>;

    private sealed class AlwaysValidValidator : AbstractValidator<TestCommand>
    {
        public AlwaysValidValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }

    private sealed class AlwaysInvalidValidator : AbstractValidator<TestCommand>
    {
        public AlwaysInvalidValidator()
        {
            RuleFor(x => x.Value).Must(_ => false).WithMessage("Always fails.");
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoValidatorsRegistered_InvokesNext()
    {
        var sut = new ValidationBehavior<TestCommand, string>([]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("ok");

        var result = await sut.Handle(new TestCommand("x"), next, CancellationToken.None);

        result.Should().Be("ok");
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_WhenAllValidatorsPass_InvokesNextAndReturnsResult()
    {
        var sut = new ValidationBehavior<TestCommand, string>([new AlwaysValidValidator()]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("ok");

        var result = await sut.Handle(new TestCommand("hello"), next, CancellationToken.None);

        result.Should().Be("ok");
        await next.Received(1)();
    }

    [Fact]
    public async Task Handle_WhenAValidatorFails_ThrowsValidationException()
    {
        var sut = new ValidationBehavior<TestCommand, string>([new AlwaysInvalidValidator()]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        var act = async () => await sut.Handle(new TestCommand("x"), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage == "Always fails."));

        await next.DidNotReceive()();
    }

    [Fact]
    public async Task Handle_WhenValidatorFails_CollectsAllErrors()
    {
        // Validator with two rules that both fail.
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Value).Must(_ => false).WithMessage("Error one.");
        validator.RuleFor(x => x.Value).Must(_ => false).WithMessage("Error two.");

        var sut = new ValidationBehavior<TestCommand, string>([validator]);
        var next = Substitute.For<RequestHandlerDelegate<string>>();

        var act = async () => await sut.Handle(new TestCommand("x"), next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().HaveCount(2);
    }
}

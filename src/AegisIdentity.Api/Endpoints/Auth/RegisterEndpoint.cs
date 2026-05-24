using AegisIdentity.Application.Auth.Register;
using AegisIdentity.CommandHandlers.Auth.Register;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Endpoints.Auth;

// POST /api/auth/register
// Creates a new inactive user account and dispatches a confirmation email.
// The email-send step is fail-open: SMTP failures are logged but do not
// abort the registration — the 201 is still returned.
public static class RegisterEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes
            .MapPost("/api/auth/register", HandleAsync)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .WithDescription(
                "Creates an inactive user account and sends a confirmation email. " +
                "The account becomes active only after the email is confirmed.")
            .WithTags("Auth")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] RegisterRequest request,
        [FromServices] IMediator mediator,
        [FromServices] IValidator<RegisterRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var command = new RegisterUserCommandHandler.Command(request.Email, request.Username, request.Password);
        var result = await mediator.Send(command, cancellationToken);

        return result switch
        {
            RegisterUserCommandHandler.Result.Success success =>
                Results.Created(
                    $"/api/users/{success.Id}",
                    new RegisterResponse(success.Id, success.Email, success.Username)),

            RegisterUserCommandHandler.Result.WeakPassword weak =>
                Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = [..weak.Errors],
                }),

            RegisterUserCommandHandler.Result.DuplicateEmail =>
                Results.Conflict(new { error = "Este email já está em uso." }),

            RegisterUserCommandHandler.Result.DuplicateUsername =>
                Results.Conflict(new { error = "Este username já está em uso." }),

            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}

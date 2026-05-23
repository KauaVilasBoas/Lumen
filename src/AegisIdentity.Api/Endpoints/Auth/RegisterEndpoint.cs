using AegisIdentity.Application.Auth.Register;
using FluentValidation;
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
        [FromServices] IRegisterUserUseCase useCase,
        [FromServices] IValidator<RegisterRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var result = await useCase.ExecuteAsync(request, cancellationToken);

        return result switch
        {
            RegisterResult.Success success =>
                Results.Created($"/api/users/{success.Response.Id}", success.Response),

            RegisterResult.WeakPassword weak =>
                Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["password"] = [..weak.Errors],
                }),

            RegisterResult.DuplicateEmail =>
                Results.Conflict(new { error = "Este email já está em uso." }),

            RegisterResult.DuplicateUsername =>
                Results.Conflict(new { error = "Este username já está em uso." }),

            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}

using AegisIdentity.Application.Auth.Login;
using AegisIdentity.CommandHandlers.Auth.Login;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AegisIdentity.Api.Endpoints.Auth;

// POST /api/auth/login
// Validates credentials, returns a short-lived JWT access token and a long-lived refresh token.
// Accounts that have not confirmed their email are rejected with 403.
// Accounts that are locked due to repeated failures are rejected with 423 Locked.
public static class LoginEndpoint
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes
            .MapPost("/api/auth/login", HandleAsync)
            .WithName("Login")
            .WithSummary("Authenticate a user and obtain access/refresh tokens")
            .WithDescription(
                "Accepts an email address or username as the identifier. " +
                "Returns a JWT access token (short-lived) and an opaque refresh token (long-lived). " +
                "Accounts locked due to repeated failed attempts receive 423 Locked.")
            .WithTags("Auth")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status423Locked);
    }

    private static async Task<IResult> HandleAsync(
        [FromBody] LoginRequest request,
        [FromServices] IMediator mediator,
        [FromServices] IValidator<LoginRequest> validator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new LoginUserCommandHandler.Command(request.Identifier, request.Password, clientIp);
        var result = await mediator.Send(command, cancellationToken);

        return result switch
        {
            LoginUserCommandHandler.Result.Success success =>
                Results.Ok(new LoginResponse(success.AccessToken, success.RefreshToken, success.ExpiresIn)),

            LoginUserCommandHandler.Result.InvalidCredentials =>
                Results.Unauthorized(),

            LoginUserCommandHandler.Result.EmailNotConfirmed =>
                Results.Forbid(),

            LoginUserCommandHandler.Result.AccountLocked =>
                Results.StatusCode(StatusCodes.Status423Locked),

            _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}

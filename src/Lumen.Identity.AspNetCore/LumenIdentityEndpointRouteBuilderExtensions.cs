using System.Security.Claims;
using Lumen.Identity.Application.Auth.ChangePassword;
using Lumen.Identity.Application.Auth.ConfirmEmail;
using Lumen.Identity.Application.Auth.ForgotPassword;
using Lumen.Identity.Application.Auth.Login;
using Lumen.Identity.Application.Auth.Logout;
using Lumen.Identity.Application.Auth.Refresh;
using Lumen.Identity.Application.Auth.Register;
using Lumen.Identity.Application.Auth.ResendConfirmationEmail;
using Lumen.Identity.Application.Auth.ResetPassword;
using Lumen.Identity.Application.Users.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.Identity.AspNetCore;

public static class LumenIdentityEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps Lumen.Identity authentication endpoints under <paramref name="prefix"/>.
    /// </summary>
    /// <remarks>
    /// Endpoints registered:
    /// <list type="bullet">
    ///   <item><description>POST   {prefix}/login</description></item>
    ///   <item><description>POST   {prefix}/refresh</description></item>
    ///   <item><description>POST   {prefix}/logout</description></item>
    ///   <item><description>POST   {prefix}/register</description></item>
    ///   <item><description>GET    {prefix}/confirm-email</description></item>
    ///   <item><description>POST   {prefix}/resend-confirmation</description></item>
    ///   <item><description>POST   {prefix}/forgot-password</description></item>
    ///   <item><description>POST   {prefix}/reset-password</description></item>
    ///   <item><description>POST   {prefix}/change-password (requires auth)</description></item>
    ///   <item><description>GET    {prefix}/me (requires auth)</description></item>
    /// </list>
    /// </remarks>
    public static IEndpointRouteBuilder MapLumenIdentityEndpoints(
        this IEndpointRouteBuilder endpoints,
        string prefix = "/api/auth")
    {
        var group = endpoints.MapGroup(prefix)
            .WithTags("Identity");

        MapAuthEndpoints(group);
        MapMeEndpoints(endpoints, prefix);

        return endpoints;
    }

    private static void MapAuthEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/login", async (LoginRequest req, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await mediator.Send(new LoginCommand(req.Identifier, req.Password, ip), ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .Produces<LoginResult>()
        .ProducesProblem(400)
        .ProducesProblem(401)
        .ProducesProblem(423);

        group.MapPost("/refresh", async (RefreshRequest req, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await mediator.Send(new RefreshTokenCommand(req.RefreshToken, ip), ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .Produces<RefreshTokenResult>()
        .ProducesProblem(400)
        .ProducesProblem(401);

        group.MapPost("/logout", async (LogoutRequest req, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserIdOrDefault(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            await mediator.Send(new LogoutCommand(req.RefreshToken, userId, ip), ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .Produces(204)
        .ProducesProblem(401);

        group.MapPost("/register", async (RegisterCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/users/{result.Id}", result);
        })
        .AllowAnonymous()
        .Produces<RegisterResult>(201)
        .ProducesProblem(400)
        .ProducesProblem(409);

        group.MapGet("/confirm-email", async (string token, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ConfirmEmailCommand(token), ct);
            return Results.Ok();
        })
        .AllowAnonymous()
        .Produces(200)
        .ProducesProblem(400)
        .ProducesProblem(401);

        group.MapPost("/resend-confirmation", async (ResendConfirmationRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ResendConfirmationEmailCommand(req.Email), ct);
            return Results.Ok();
        })
        .AllowAnonymous()
        .Produces(200)
        .ProducesProblem(400);

        group.MapPost("/forgot-password", async (ForgotPasswordRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ForgotPasswordCommand(req.Email), ct);
            return Results.Ok();
        })
        .AllowAnonymous()
        .Produces(200)
        .ProducesProblem(400);

        group.MapPost("/reset-password", async (ResetPasswordRequest req, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new ResetPasswordCommand(req.Token, req.NewPassword), ct);
            return Results.NoContent();
        })
        .AllowAnonymous()
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }

    private static void MapMeEndpoints(IEndpointRouteBuilder endpoints, string authPrefix)
    {
        var mePrefix = authPrefix.TrimEnd('/').Replace("/auth", "/me");
        if (mePrefix == authPrefix.TrimEnd('/'))
            mePrefix = "/api/me";

        var meGroup = endpoints.MapGroup(mePrefix)
            .WithTags("Identity")
            .RequireAuthorization();

        meGroup.MapGet("/", async (HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserIdOrDefault(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            var result = await mediator.Send(new GetCurrentUserQuery(userId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        })
        .Produces<GetCurrentUserResult>()
        .ProducesProblem(401)
        .ProducesProblem(404);

        meGroup.MapPost("/change-password", async (ChangePasswordRequest req, HttpContext ctx, IMediator mediator, CancellationToken ct) =>
        {
            var userId = GetUserIdOrDefault(ctx);
            if (userId == Guid.Empty) return Results.Unauthorized();

            await mediator.Send(new ChangePasswordCommand(userId, req.CurrentPassword, req.NewPassword), ct);
            return Results.NoContent();
        })
        .Produces(204)
        .ProducesProblem(400)
        .ProducesProblem(401);
    }

    private static Guid GetUserIdOrDefault(HttpContext ctx)
    {
        var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    // Request record types used by the endpoints (equivalent to the API controller request models)
    private sealed record LoginRequest(string Identifier, string Password);
    private sealed record RefreshRequest(string RefreshToken);
    private sealed record LogoutRequest(string? RefreshToken);
    private sealed record ResendConfirmationRequest(string Email);
    private sealed record ForgotPasswordRequest(string Email);
    private sealed record ResetPasswordRequest(string Token, string NewPassword);
    private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}

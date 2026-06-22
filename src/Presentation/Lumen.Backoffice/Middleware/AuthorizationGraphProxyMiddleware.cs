using System.Net;
using System.Net.WebSockets;
using System.Security.Claims;
using Lumen.Backoffice.Configuration;
using Lumen.Domain.Authorization;
using Lumen.SharedKernel.Constants;
using Microsoft.Extensions.Options;

namespace Lumen.Backoffice.Middleware;

public sealed class AuthorizationGraphProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BackofficeApiOptions _apiOptions;
    private readonly ILogger<AuthorizationGraphProxyMiddleware> _logger;

    public AuthorizationGraphProxyMiddleware(
        RequestDelegate next,
        IOptions<BackofficeApiOptions> apiOptions,
        ILogger<AuthorizationGraphProxyMiddleware> logger)
    {
        _next = next;
        _apiOptions = apiOptions.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserPermissionService permissionService)
    {
        if (!context.Request.Path.StartsWithSegments(HubRoutes.AuthorizationGraph))
        {
            await _next(context);
            return;
        }

        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!await CallerHasPermissionAsync(context.User, permissionService))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var accessToken = context.User.FindFirstValue(BackofficeClaimTypes.AccessToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await ProxyWebSocketAsync(context, accessToken);
    }

    private async Task ProxyWebSocketAsync(HttpContext context, string accessToken)
    {
        var apiBaseUri = new Uri(_apiOptions.BaseUrl);
        var wsScheme = apiBaseUri.Scheme == "https" ? "wss" : "ws";
        var upstreamUri = new UriBuilder(
            wsScheme,
            apiBaseUri.Host,
            apiBaseUri.Port,
            HubRoutes.AuthorizationGraph)
        {
            Query = context.Request.QueryString.Value ?? string.Empty
        }.Uri;

        using var upstreamSocket = new ClientWebSocket();
        upstreamSocket.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");

        foreach (var protocol in context.WebSockets.WebSocketRequestedProtocols)
            upstreamSocket.Options.AddSubProtocol(protocol);

        try
        {
            await upstreamSocket.ConnectAsync(upstreamUri, context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to upstream hub at {Uri}.", upstreamUri);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            return;
        }

        using var clientSocket = await context.WebSockets.AcceptWebSocketAsync(
            upstreamSocket.SubProtocol);

        await BidirectionalRelayAsync(clientSocket, upstreamSocket, context.RequestAborted);
    }

    private static async Task BidirectionalRelayAsync(
        WebSocket clientSocket,
        ClientWebSocket upstreamSocket,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var clientToUpstream = RelayAsync(clientSocket, upstreamSocket, linked.Token);
        var upstreamToClient = RelayAsync(upstreamSocket, clientSocket, linked.Token);

        await Task.WhenAny(clientToUpstream, upstreamToClient);
        linked.Cancel();

        await CloseIfOpenAsync(clientSocket);
        await CloseIfOpenAsync(upstreamSocket);
    }

    private static async Task RelayAsync(
        WebSocket source,
        WebSocket destination,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[WebSocketBufferSize.Default];

        while (source.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var result = await source.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                if (destination.State == WebSocketState.Open)
                    await destination.CloseAsync(
                        result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription,
                        CancellationToken.None);
                break;
            }

            if (destination.State == WebSocketState.Open)
                await destination.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    cancellationToken);
        }
    }

    private static async Task CloseIfOpenAsync(WebSocket socket)
    {
        if (socket.State == WebSocketState.Open)
        {
            try
            {
                await socket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    string.Empty,
                    CancellationToken.None);
            }
            catch
            {
                // socket may already be aborted
            }
        }
    }

    private static async Task<bool> CallerHasPermissionAsync(
        ClaimsPrincipal user,
        IUserPermissionService permissionService)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var userId))
            return false;

        return await permissionService.HasPermissionAsync(userId, PermissionCodes.AuthorizationGraph.View);
    }
}

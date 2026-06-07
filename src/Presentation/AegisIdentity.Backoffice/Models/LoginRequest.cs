namespace AegisIdentity.Backoffice.Models;

/// <summary>
/// Payload sent to <c>POST /api/auth/login</c> on the AegisIdentity Api.
/// Fields mirror <c>LoginUserCommandHandler.Command</c> (identifier + password).
/// ClientIp is resolved server-side in the Api controller — not sent from Backoffice.
/// </summary>
public sealed record LoginRequest(string Identifier, string Password);
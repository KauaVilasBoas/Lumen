namespace AegisIdentity.Application.Auth.Register;

public sealed record RegisterRequest(string Email, string Username, string Password);

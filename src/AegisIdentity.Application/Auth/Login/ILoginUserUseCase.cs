namespace AegisIdentity.Application.Auth.Login;

public interface ILoginUserUseCase
{
    Task<LoginResult> ExecuteAsync(LoginRequest request, string clientIp, CancellationToken ct = default);
}

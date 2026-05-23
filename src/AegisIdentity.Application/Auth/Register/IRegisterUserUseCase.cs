namespace AegisIdentity.Application.Auth.Register;

public interface IRegisterUserUseCase
{
    Task<RegisterResult> ExecuteAsync(RegisterRequest request, CancellationToken ct = default);
}

namespace AegisIdentity.Application.Auth.Register;

public abstract class RegisterResult
{
    private RegisterResult() { }

    public sealed class Success : RegisterResult
    {
        public RegisterResponse Response { get; }

        public Success(RegisterResponse response)
        {
            Response = response;
        }
    }

    public sealed class WeakPassword : RegisterResult
    {
        public IReadOnlyList<string> Errors { get; }

        public WeakPassword(IReadOnlyList<string> errors)
        {
            Errors = errors;
        }
    }

    public sealed class DuplicateEmail : RegisterResult { }

    public sealed class DuplicateUsername : RegisterResult { }
}

namespace AegisIdentity.Application.Auth.Login;

public abstract class LoginResult
{
    private LoginResult() { }

    public sealed class Success : LoginResult
    {
        public LoginResponse Response { get; }

        public Success(LoginResponse response)
        {
            Response = response;
        }
    }

    /// <summary>
    /// Credential is wrong or the user does not exist.
    /// Intentionally opaque — do not reveal whether the identifier was found.
    /// </summary>
    public sealed class InvalidCredentials : LoginResult { }

    /// <summary>
    /// The account has not yet confirmed its email address.
    /// </summary>
    public sealed class EmailNotConfirmed : LoginResult { }

    /// <summary>
    /// The account is temporarily locked due to repeated failed attempts.
    /// </summary>
    public sealed class AccountLocked : LoginResult
    {
        public DateTime LockedUntil { get; }

        public AccountLocked(DateTime lockedUntil)
        {
            LockedUntil = lockedUntil;
        }
    }
}

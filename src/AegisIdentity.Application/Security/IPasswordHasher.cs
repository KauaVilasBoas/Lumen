namespace AegisIdentity.Application.Security;

public interface IPasswordHasher
{
    string Hash(string plainText);

    bool Verify(string plainText, string hash);
}

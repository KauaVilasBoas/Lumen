namespace Lumen.Modules.Identity.Domain.Security;

internal interface IPasswordHasher
{
    string Hash(string plainText);

    bool Verify(string plainText, string hash);
}

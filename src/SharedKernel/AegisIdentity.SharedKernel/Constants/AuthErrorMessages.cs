namespace AegisIdentity.SharedKernel.Constants;

public static class AuthErrorMessages
{
    public const string EmailAlreadyInUse = "Este email já está em uso.";
    public const string UsernameAlreadyInUse = "Este username já está em uso.";

    public const string EmailRequired = "O campo email é obrigatório.";
    public const string EmailInvalid = "O email informado não é válido.";
    public const string EmailTooLong = "O email deve ter no máximo 256 caracteres.";

    public const string UsernameRequired = "O campo username é obrigatório.";
    public const string UsernameTooShort = "O username deve ter no mínimo {0} caracteres.";
    public const string UsernameTooLong = "O username deve ter no máximo {0} caracteres.";
    public const string UsernameInvalidChars = "O username deve conter apenas letras, números, underscores ou hífens.";

    public const string PasswordRequired = "O campo senha é obrigatório.";

    public const string PasswordTooShort = "A senha deve ter no mínimo 12 caracteres.";
    public const string PasswordMissingUppercase = "A senha deve conter pelo menos uma letra maiúscula.";
    public const string PasswordMissingLowercase = "A senha deve conter pelo menos uma letra minúscula.";
    public const string PasswordMissingDigit = "A senha deve conter pelo menos um dígito.";
    public const string PasswordMissingSpecial = "A senha deve conter pelo menos um caractere especial.";
    public const string PasswordEqualsIdentity = "A senha não pode ser igual ao seu email/username.";
    public const string PasswordPwned = "Esta senha aparece em vazamentos públicos conhecidos. Escolha outra.";
}

namespace Lumen.SharedKernel.Constants;

public static class BackofficeErrorMessages
{
    public const string InvalidCredentials   = "Identificador ou senha inválidos.";
    public const string EmailNotConfirmed    = "Confirme seu endereço de email antes de acessar o backoffice.";
    public const string AccountLocked        = "Conta temporariamente bloqueada por tentativas excessivas. Tente novamente mais tarde.";
    public const string ApiCommunicationError = "Erro ao comunicar com a Api. Tente novamente.";

    public const string SystemProfileCannotBeDeleted = "Perfis de sistema não podem ser removidos.";

    public const string CreateProfileError    = "Erro ao criar perfil.";
    public const string UpdateProfileError    = "Erro ao atualizar perfil.";
    public const string DeleteProfileError    = "Erro ao remover perfil.";
    public const string SetPermissionsError   = "Erro ao definir permissões do perfil.";
    public const string AssignProfileError    = "Erro ao atribuir perfil.";
    public const string RemoveProfileError    = "Erro ao remover perfil.";
}

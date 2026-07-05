namespace Lumen.Authorization.Internal;

internal static class AuthorizationErrorMessages
{
    public const string SystemProfileCannotBeDeleted = "Perfis de sistema não podem ser removidos.";

    public const string ConnectionStringNullOrEmpty =
        "Lumen.Authorization requer uma connection string não vazia. " +
        "Forneça uma string não vazia em AddLumenAuthorization(connectionString) " +
        "ou configure ConnectionStrings:DefaultConnection na IConfiguration.";

    public const string ConnectionStringNotSqlServer =
        "Lumen.Authorization requer uma connection string SQL Server válida. " +
        "A string fornecida não pôde ser interpretada como SQL Server. " +
        "Verifique o formato (ex.: \"Server=...;Database=...;Trusted_Connection=True;\").";

    public const string ConnectionStringNotPostgres =
        "Lumen.Authorization requer uma connection string PostgreSQL válida quando Provider=PostgreSQL. " +
        "A string fornecida não pôde ser interpretada como Npgsql. " +
        "Verifique o formato (ex.: \"Host=localhost;Database=lumen;Username=postgres;Password=...\").";

    public const string UnknownProvider =
        "DatabaseProvider desconhecido. Use DatabaseProvider.SqlServer ou DatabaseProvider.PostgreSQL.";
}

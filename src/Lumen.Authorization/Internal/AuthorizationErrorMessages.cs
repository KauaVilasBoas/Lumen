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

    public const string MissingPermissionsInCatalog =
        "Lumen.Authorization: os seguintes permission codes são declarados via [RequirePermission] mas " +
        "não estão semeados no banco de dados: {0}. " +
        "Seed them via MigrationBuilder.SeedLumenPermission() in a consumer migration. " +
        "Set FailFastOnMissingPermission = false to log a warning instead of aborting startup.";
}

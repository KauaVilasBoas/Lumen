using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Authorization.Migrations;

public static class MigrationBuilderExtensions
{
    private const string PermissionTable = "[Lumen].[Permission]";
    private const string PermissionGroupTable = "[Lumen].[PermissionGroup]";

    public static void SeedLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        migrationBuilder.Sql($"""
            IF NOT EXISTS (SELECT 1 FROM {PermissionGroupTable} WHERE [Name] = N'{Escape(name)}')
            BEGIN
                INSERT INTO {PermissionGroupTable} ([Id], [Name], [Description], [IsDeleted], [DeletedAt])
                VALUES (NEWID(), N'{Escape(name)}', N'{Escape(description)}', 0, NULL)
            END
            """);
    }

    public static void SeedLumenPermission(
        this MigrationBuilder migrationBuilder,
        string code,
        string displayName,
        string? groupName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var groupLookup = groupName is not null
            ? $"(SELECT TOP 1 [Id] FROM {PermissionGroupTable} WHERE [Name] = N'{Escape(groupName)}')"
            : "NULL";

        migrationBuilder.Sql($"""
            IF NOT EXISTS (SELECT 1 FROM {PermissionTable} WHERE [Code] = N'{Escape(code)}')
            BEGIN
                INSERT INTO {PermissionTable} ([Id], [Code], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt])
                VALUES (NEWID(), N'{Escape(code)}', N'{Escape(displayName)}', {groupLookup}, 0, NULL)
            END
            """);
    }

    public static void DeleteLumenPermission(
        this MigrationBuilder migrationBuilder,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        migrationBuilder.Sql($"""
            DELETE FROM {PermissionTable} WHERE [Code] = N'{Escape(code)}'
            """);
    }

    public static void DeleteLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        migrationBuilder.Sql($"""
            DELETE FROM {PermissionGroupTable} WHERE [Name] = N'{Escape(name)}'
            """);
    }

    private static string Escape(string value) => value.Replace("'", "''");
}

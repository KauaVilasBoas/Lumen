using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Authorization.Migrations;

public static class MigrationBuilderExtensions
{
    private const string PermissionsTable = "[Lumen].[Permissions]";
    private const string GroupPermissionsTable = "[Lumen].[GroupPermissions]";

    public static void SeedLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name,
        string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        migrationBuilder.Sql($"""
            IF NOT EXISTS (SELECT 1 FROM {GroupPermissionsTable} WHERE [Name] = N'{Escape(name)}')
            BEGIN
                INSERT INTO {GroupPermissionsTable} ([Id], [Name], [DisplayName], [IsDeleted], [DeletedAt])
                VALUES (NEWID(), N'{Escape(name)}', N'{Escape(displayName)}', 0, NULL)
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

        var parts = code.Split('.', 2);
        var controller = parts.Length == 2 ? parts[0] : code;
        var action = parts.Length == 2 ? parts[1] : code;

        var groupLookup = groupName is not null
            ? $"(SELECT TOP 1 [Id] FROM {GroupPermissionsTable} WHERE [Name] = N'{Escape(groupName)}')"
            : "NULL";

        migrationBuilder.Sql($"""
            IF NOT EXISTS (SELECT 1 FROM {PermissionsTable} WHERE [Code] = N'{Escape(code)}')
            BEGIN
                INSERT INTO {PermissionsTable} ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsOrphan], [OrphanedAt], [IsDeleted], [DeletedAt])
                VALUES (NEWID(), N'{Escape(code)}', N'{Escape(controller)}', N'{Escape(action)}', N'{Escape(displayName)}', {groupLookup}, 0, NULL, 0, NULL)
            END
            """);
    }

    public static void DeleteLumenPermission(
        this MigrationBuilder migrationBuilder,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        migrationBuilder.Sql($"""
            DELETE FROM {PermissionsTable} WHERE [Code] = N'{Escape(code)}'
            """);
    }

    public static void DeleteLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        migrationBuilder.Sql($"""
            DELETE FROM {GroupPermissionsTable} WHERE [Name] = N'{Escape(name)}'
            """);
    }

    private static string Escape(string value) => value.Replace("'", "''");
}

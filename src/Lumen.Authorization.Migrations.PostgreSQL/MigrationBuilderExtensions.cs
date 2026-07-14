using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Authorization.Migrations.PostgreSQL;

public static class MigrationBuilderExtensions
{
    private const string PermissionsTable = "\"Lumen\".\"Permissions\"";
    private const string GroupPermissionsTable = "\"Lumen\".\"GroupPermissions\"";

    public static void SeedLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name,
        string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        migrationBuilder.Sql($"""
            INSERT INTO {GroupPermissionsTable} ("Id", "Name", "DisplayName", "IsDeleted", "DeletedAt")
            SELECT gen_random_uuid(), '{Escape(name)}', '{Escape(displayName)}', false, NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM {GroupPermissionsTable} WHERE "Name" = '{Escape(name)}'
            );
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
            ? $"(SELECT \"Id\" FROM {GroupPermissionsTable} WHERE \"Name\" = '{Escape(groupName)}' LIMIT 1)"
            : "NULL";

        migrationBuilder.Sql($"""
            INSERT INTO {PermissionsTable} ("Id", "Code", "Controller", "Action", "DisplayName", "GroupPermissionId", "IsOrphan", "OrphanedAt", "IsDeleted", "DeletedAt")
            SELECT gen_random_uuid(), '{Escape(code)}', '{Escape(controller)}', '{Escape(action)}', '{Escape(displayName)}', {groupLookup}, false, NULL, false, NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM {PermissionsTable} WHERE "Code" = '{Escape(code)}'
            );
            """);
    }

    public static void DeleteLumenPermission(
        this MigrationBuilder migrationBuilder,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        migrationBuilder.Sql($"""
            DELETE FROM {PermissionsTable} WHERE "Code" = '{Escape(code)}';
            """);
    }

    public static void DeleteLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        migrationBuilder.Sql($"""
            DELETE FROM {GroupPermissionsTable} WHERE "Name" = '{Escape(name)}';
            """);
    }

    private static string Escape(string value) => value.Replace("'", "''");
}

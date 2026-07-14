using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Authorization.Migrations.PostgreSQL;

public static class MigrationBuilderExtensions
{
    private const string PermissionTable = "\"Lumen\".\"Permission\"";
    private const string PermissionGroupTable = "\"Lumen\".\"PermissionGroup\"";

    public static void SeedLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name,
        string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        migrationBuilder.Sql($"""
            INSERT INTO {PermissionGroupTable} ("Id", "Name", "Description", "IsDeleted", "DeletedAt")
            SELECT gen_random_uuid(), '{Escape(name)}', '{Escape(description)}', false, NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM {PermissionGroupTable} WHERE "Name" = '{Escape(name)}'
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

        var groupLookup = groupName is not null
            ? $"(SELECT \"Id\" FROM {PermissionGroupTable} WHERE \"Name\" = '{Escape(groupName)}' LIMIT 1)"
            : "NULL";

        migrationBuilder.Sql($"""
            INSERT INTO {PermissionTable} ("Id", "Code", "DisplayName", "GroupPermissionId", "IsDeleted", "DeletedAt")
            SELECT gen_random_uuid(), '{Escape(code)}', '{Escape(displayName)}', {groupLookup}, false, NULL
            WHERE NOT EXISTS (
                SELECT 1 FROM {PermissionTable} WHERE "Code" = '{Escape(code)}'
            );
            """);
    }

    public static void DeleteLumenPermission(
        this MigrationBuilder migrationBuilder,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        migrationBuilder.Sql($"""
            DELETE FROM {PermissionTable} WHERE "Code" = '{Escape(code)}';
            """);
    }

    public static void DeleteLumenPermissionGroup(
        this MigrationBuilder migrationBuilder,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        migrationBuilder.Sql($"""
            DELETE FROM {PermissionGroupTable} WHERE "Name" = '{Escape(name)}';
            """);
    }

    private static string Escape(string value) => value.Replace("'", "''");
}

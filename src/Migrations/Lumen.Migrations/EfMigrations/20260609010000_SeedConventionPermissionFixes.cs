using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Migrations.EfMigrations
{
    public partial class SeedConventionPermissionFixes : Migration
    {
        private const string UsersGroupId              = "40000000-0000-0000-0000-000000000010";
        private const string AuditGroupId              = "40000000-0000-0000-0000-000000000011";
        private const string DiagnosticsGroupId        = "40000000-0000-0000-0000-000000000012";

        private const string UsersGetPermissionId           = "40000000-0000-0000-0000-000000000020";
        private const string AuditReadPermissionId          = "40000000-0000-0000-0000-000000000021";
        private const string DiagnosticsGetCacheStatsPermId = "40000000-0000-0000-0000-000000000022";
        private const string DiagnosticsGetJobStatsPermId   = "40000000-0000-0000-0000-000000000023";

        private const string UsersGroupName       = "Users";
        private const string AuditGroupName       = "Audit";
        private const string DiagnosticsGroupName = "Diagnostics";

        private const string UsersGetCode                = "Users.Get";
        private const string AuditReadCode               = "Audit.Read";
        private const string DiagnosticsGetCacheStatsCode = "Diagnostics.GetCacheStats";
        private const string DiagnosticsGetJobStatsCode   = "Diagnostics.GetJobStats";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [GroupPermissions] WHERE [Name] = N'{UsersGroupName}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [GroupPermissions] ([Id], [Name], [Description], [IsDeleted], [DeletedAt])
    VALUES ('{UsersGroupId}', N'{UsersGroupName}', N'{UsersGroupName}', 0, NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [GroupPermissions] WHERE [Name] = N'{AuditGroupName}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [GroupPermissions] ([Id], [Name], [Description], [IsDeleted], [DeletedAt])
    VALUES ('{AuditGroupId}', N'{AuditGroupName}', N'{AuditGroupName}', 0, NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [GroupPermissions] WHERE [Name] = N'{DiagnosticsGroupName}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [GroupPermissions] ([Id], [Name], [Description], [IsDeleted], [DeletedAt])
    VALUES ('{DiagnosticsGroupId}', N'{DiagnosticsGroupName}', N'{DiagnosticsGroupName}', 0, NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{UsersGetCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{UsersGetPermissionId}',
        N'{UsersGetCode}',
        N'Users',
        N'Get',
        N'Users — Get',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{UsersGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{AuditReadCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{AuditReadPermissionId}',
        N'{AuditReadCode}',
        N'Audit',
        N'Read',
        N'Audit — Read',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{AuditGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{DiagnosticsGetCacheStatsCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{DiagnosticsGetCacheStatsPermId}',
        N'{DiagnosticsGetCacheStatsCode}',
        N'Diagnostics',
        N'GetCacheStats',
        N'Diagnostics — GetCacheStats',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{DiagnosticsGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{DiagnosticsGetJobStatsCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{DiagnosticsGetJobStatsPermId}',
        N'{DiagnosticsGetJobStatsCode}',
        N'Diagnostics',
        N'GetJobStats',
        N'Diagnostics — GetJobStats',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{DiagnosticsGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [Permissions] WHERE [Id] IN (
    '{UsersGetPermissionId}',
    '{AuditReadPermissionId}',
    '{DiagnosticsGetCacheStatsPermId}',
    '{DiagnosticsGetJobStatsPermId}'
);
DELETE FROM [GroupPermissions] WHERE [Id] IN (
    '{UsersGroupId}',
    '{AuditGroupId}',
    '{DiagnosticsGroupId}'
);");
        }
    }
}

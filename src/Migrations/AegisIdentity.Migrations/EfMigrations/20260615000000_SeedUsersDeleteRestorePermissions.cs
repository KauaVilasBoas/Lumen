using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisIdentity.Migrations.EfMigrations
{
    public partial class SeedUsersDeleteRestorePermissions : Migration
    {
        private const string UsersGroupId               = "40000000-0000-0000-0000-000000000010";
        private const string UsersDeletePermissionId    = "40000000-0000-0000-0000-000000000025";
        private const string UsersRestorePermissionId   = "40000000-0000-0000-0000-000000000026";

        private const string UsersGroupName      = "Users";
        private const string UsersDeleteCode     = "Users.Delete";
        private const string UsersRestoreCode    = "Users.Restore";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{UsersDeleteCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{UsersDeletePermissionId}',
        N'{UsersDeleteCode}',
        N'Users',
        N'Delete',
        N'Users — Delete',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{UsersGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;");

            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{UsersRestoreCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{UsersRestorePermissionId}',
        N'{UsersRestoreCode}',
        N'Users',
        N'Restore',
        N'Users — Restore',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{UsersGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [Permissions] WHERE [Id] IN ('{UsersDeletePermissionId}', '{UsersRestorePermissionId}');");
        }
    }
}

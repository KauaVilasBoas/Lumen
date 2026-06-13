using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisIdentity.Migrations.EfMigrations
{
    public partial class SeedUsersUpdatePermission : Migration
    {
        private const string UsersGroupId              = "40000000-0000-0000-0000-000000000010";
        private const string UsersUpdatePermissionId   = "40000000-0000-0000-0000-000000000024";

        private const string UsersGroupName      = "Users";
        private const string UsersUpdateCode     = "Users.Update";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{UsersUpdateCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{UsersUpdatePermissionId}',
        N'{UsersUpdateCode}',
        N'Users',
        N'Update',
        N'Users — Update',
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
DELETE FROM [Permissions] WHERE [Id] = '{UsersUpdatePermissionId}';");
        }
    }
}

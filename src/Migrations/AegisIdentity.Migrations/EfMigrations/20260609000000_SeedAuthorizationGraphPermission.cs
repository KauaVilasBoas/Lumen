using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AegisIdentity.Migrations.EfMigrations
{
    public partial class SeedAuthorizationGraphPermission : Migration
    {
        private const string AuthorizationGroupId = "40000000-0000-0000-0000-000000000001";
        private const string AuthorizationGraphViewPermissionId = "40000000-0000-0000-0000-000000000002";
        private const string AuthorizationGroupName = "Authorization";
        private const string AuthorizationGraphViewCode = "AuthorizationGraph.View";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
IF NOT EXISTS (SELECT 1 FROM [GroupPermissions] WHERE [Name] = N'{AuthorizationGroupName}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [GroupPermissions] ([Id], [Name], [Description], [IsDeleted], [DeletedAt])
    VALUES ('{AuthorizationGroupId}', N'{AuthorizationGroupName}', N'{AuthorizationGroupName}', 0, NULL);
END;

IF NOT EXISTS (SELECT 1 FROM [Permissions] WHERE [Code] = N'{AuthorizationGraphViewCode}' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [Permissions] ([Id], [Code], [Controller], [Action], [DisplayName], [GroupPermissionId], [IsDeleted], [DeletedAt], [IsOrphan], [OrphanedAt])
    VALUES (
        '{AuthorizationGraphViewPermissionId}',
        N'{AuthorizationGraphViewCode}',
        N'AuthorizationGraph',
        N'View',
        N'AuthorizationGraph — View',
        (SELECT TOP 1 [Id] FROM [GroupPermissions] WHERE [Name] = N'{AuthorizationGroupName}' AND [IsDeleted] = 0),
        0,
        NULL,
        0,
        NULL);
END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
DELETE FROM [Permissions] WHERE [Id] = '{AuthorizationGraphViewPermissionId}';
DELETE FROM [GroupPermissions] WHERE [Id] = '{AuthorizationGroupId}';");
        }
    }
}
